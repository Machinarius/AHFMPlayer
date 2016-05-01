using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Debug = System.Diagnostics.Debug;

// TODO: Implement ideas from https://blog.xamarin.com/background-audio-streaming-with-xamarin-android/
namespace AHFMPlayer.Controllers {
  [Service]
  public class PlaybackService : Service {
    const string StreamFileUrl = "http://us2.ah.fm:443";

    public enum PlaybackStatus {
      Stopped,
      Error,
      Buffering,
      Playback
    }

    public event EventHandler<PlaybackStatus> PlaybackStatusChanged;

    private PlaybackStatus _status;
    public PlaybackStatus Status {
      get {
        return _status;
      }
      set {
        _status = value;
        PlaybackStatusChanged?.Invoke(this, _status);
      }
    }

    private MediaPlayer mediaPlayer;

    public override IBinder OnBind(Intent intent) {
      return new Controller(this);
    }
    
    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId) {
      base.OnStartCommand(intent, flags, startId);

#pragma warning disable 4014
      InitializeMediaPlayer();
#pragma warning restore 4014
      return StartCommandResult.Sticky;
    }

    public override void OnDestroy() {
      base.OnDestroy();
      DestroyMediaPlayer();
    }
    
    private async Task InitializeMediaPlayer() {
      if (mediaPlayer != null) {
        Debug.WriteLine("Warning: Cannot init the media player more than once");
        return;
      }

      mediaPlayer = new MediaPlayer();
      mediaPlayer.Prepared += OnMediaPlayerPrepared;
      mediaPlayer.Error += OnMediaPlayerError;
      mediaPlayer.SetAudioStreamType(Stream.Music);

      try {
        await mediaPlayer.SetDataSourceAsync(StreamFileUrl);
      } catch (Exception ex) {
        Debug.WriteLine("Exception setting data source");
        Debug.WriteLine(ex);

        Status = PlaybackStatus.Error;
        DestroyMediaPlayer();
        
        return;
      }
    }

    private void DestroyMediaPlayer() {
      mediaPlayer.Prepared -= OnMediaPlayerPrepared;
      mediaPlayer.Error -= OnMediaPlayerError;

      mediaPlayer.Stop();
      mediaPlayer.Release();
      mediaPlayer.Dispose();
      mediaPlayer = null;
    }

    public async void Play() {
      if (mediaPlayer == null) {
        await InitializeMediaPlayer();
      }

      mediaPlayer.PrepareAsync();
      Status = PlaybackStatus.Buffering;
    }

    public void Stop() {
      mediaPlayer.Stop();
      Status = PlaybackStatus.Stopped;
    }

    private void OnMediaPlayerPrepared(object sender, EventArgs e) {
      mediaPlayer.Start();
      Status = PlaybackStatus.Playback;
    }

    private void OnMediaPlayerError(object sender, MediaPlayer.ErrorEventArgs e) {
      DestroyMediaPlayer();
      Status = PlaybackStatus.Error;
    }

    public class Controller : Binder {
      private readonly PlaybackService service;

      public event EventHandler<PlaybackStatus> PlaybackStatusChanged;

      public PlaybackStatus PlaybackStatus {
        get {
          return service.Status;
        }
      }

      public Controller(PlaybackService service) {
        if (service == null) {
          throw new ArgumentNullException(nameof(service));
        }

        this.service = service;

        service.PlaybackStatusChanged += (source, status) => {
          PlaybackStatusChanged?.Invoke(source, status);
        };
      }

      public void Play() {
        service.Play();
      }

      public void Stop() {
        service.Stop();
      }
    }
  }
}
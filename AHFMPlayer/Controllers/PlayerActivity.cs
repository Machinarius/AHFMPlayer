using System;
using Android.App;
using Android.Media;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Com.Lilarcor.Cheeseknife;
using Debug = System.Diagnostics.Debug;

namespace AHFMPlayer.Controllers {
  [Activity(MainLauncher = true, Theme = "@style/Theme.AppCompat.Light")]
  public class PlayerActivity : AppCompatActivity {
    const string StreamFileUrl = "http://us2.ah.fm:443";

    [InjectView(Resource.Id.PlayButton)]
    private Button playButton;

    [InjectView(Resource.Id.StopButton)]
    private Button stopButton;

    [InjectView(Resource.Id.PlaybackStatusLabel)]
    private TextView statusLabel;
    
    private MediaPlayer mediaPlayer;

    private bool initializingMP;

    protected override void OnCreate(Bundle savedInstanceState) {
      base.OnCreate(savedInstanceState);
      SetContentView(Resource.Layout.Player);
      Cheeseknife.Inject(this);

      UpdateUIState();
    }

    // This is a bad practice. 
    // Async methods in activities may cause memory leaks and app crashes
    // because the callback may be invoked on activity instances that are
    // destroyed. A rotation while an async operation may trigger this
    // behaviour, among many other causes.
    private async void InitMediaPlayer() {
      if (mediaPlayer != null) {
        return;
      }

      initializingMP = true;
      mediaPlayer = new MediaPlayer();
      mediaPlayer.SetAudioStreamType(Stream.Music);

      try {
        await mediaPlayer.SetDataSourceAsync(StreamFileUrl);
      } catch (Exception ex) {
        Debug.WriteLine("Exception setting data source");
        Debug.WriteLine(ex);

        statusLabel.Text = GetString(Resource.String.PlaybackError);

        mediaPlayer.Release();
        mediaPlayer.Dispose();
        mediaPlayer = null;

        initializingMP = false;
        return;
      }

      mediaPlayer.Prepared += OnMediaPlayerPrepared;
      mediaPlayer.PrepareAsync();
      initializingMP = false;
    }

    private void OnMediaPlayerPrepared(object sender, EventArgs e) {
      mediaPlayer.Start();
      UpdateUIState();
    }

    private void UpdateUIState() {
      if (initializingMP) {
        statusLabel.Text = GetString(Resource.String.Buffering);
        playButton.Enabled = false;
        stopButton.Enabled = false;
        return;
      }

      if (mediaPlayer == null) {
        statusLabel.Text = GetString(Resource.String.Ready);
        playButton.Enabled = true;
        stopButton.Enabled = false;
        return;
      }

      if (mediaPlayer.IsPlaying) {
        statusLabel.Text = GetString(Resource.String.NowPlaying);
        playButton.Enabled = false;
        stopButton.Enabled = true;
      } else {
        statusLabel.Text = GetString(Resource.String.Ready);
        playButton.Enabled = true;
        stopButton.Enabled = false;
      }
    }

    [InjectOnClick(Resource.Id.PlayButton)]
    public void OnPlayButttonClicked(object source, EventArgs e) {
      if (mediaPlayer == null) {
        InitMediaPlayer();
      } else {
        mediaPlayer.Start();
        UpdateUIState();
      }
    }

    [InjectOnClick(Resource.Id.StopButton)]
    public void OnStopButtonClicked(object source, EventArgs e) {
      if (mediaPlayer == null) {
        throw new InvalidOperationException("Cannot stop a null media player");
      }

      mediaPlayer.Stop();
      UpdateUIState();
    }
  }
}
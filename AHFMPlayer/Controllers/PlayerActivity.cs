using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Com.Lilarcor.Cheeseknife;

namespace AHFMPlayer.Controllers {
  [Activity(MainLauncher = true, Theme = "@style/Theme.AppCompat.Light")]
  public class PlayerActivity : AppCompatActivity, IServiceConnection {
#pragma warning disable 0649
    [InjectView(Resource.Id.PlayButton)]
    private Button playButton;

    [InjectView(Resource.Id.StopButton)]
    private Button stopButton;

    [InjectView(Resource.Id.PlaybackStatusLabel)]
    private TextView statusLabel;
#pragma warning restore 0649

    private PlaybackService.Controller playbackController;

    protected override void OnCreate(Bundle savedInstanceState) {
      base.OnCreate(savedInstanceState);
      SetContentView(Resource.Layout.Player);
      Cheeseknife.Inject(this);

      var playbackServiceIntent = new Intent(this, typeof(PlaybackService));
      StartService(playbackServiceIntent);
      BindService(playbackServiceIntent, this, 0);
    }

    public void OnServiceConnected(ComponentName name, IBinder service) {
      playbackController = (PlaybackService.Controller)service;
      playbackController.PlaybackStatusChanged += OnPlaybackStatusChanged;

      UpdateUIState();
    }

    public void OnServiceDisconnected(ComponentName name) {
      playbackController.PlaybackStatusChanged -= OnPlaybackStatusChanged;
      playbackController = null;
    }

    private void OnPlaybackStatusChanged(object sender, PlaybackService.PlaybackStatus e) {
      UpdateUIState();
    }

    private void EnsurePlaybackControllerAvailable() {
      if (playbackController == null) {
        throw new InvalidOperationException("Cannot operate on a null playback controller");
      }
    }

    private void UpdateUIState() {
      EnsurePlaybackControllerAvailable();

      int labelMessageId;
      bool playEnabled;
      bool stopEnabled;

      switch (playbackController.PlaybackStatus) {
        case PlaybackService.PlaybackStatus.Error:
          labelMessageId = Resource.String.PlaybackError;
          playEnabled = true;
          stopEnabled = false;
          break;
        case PlaybackService.PlaybackStatus.Buffering:
          labelMessageId = Resource.String.Buffering;
          playEnabled = false;
          stopEnabled = false;
          break;
        case PlaybackService.PlaybackStatus.Playback:
          labelMessageId = Resource.String.NowPlaying;
          playEnabled = false;
          stopEnabled = true;
          break;
        case PlaybackService.PlaybackStatus.Stopped:
          labelMessageId = Resource.String.Ready;
          playEnabled = true;
          stopEnabled = false;
          break;
        default:
          throw new InvalidOperationException("Invalid playback status received. Bug?");
      }

      statusLabel.Text = GetString(labelMessageId);
      playButton.Enabled = playEnabled;
      stopButton.Enabled = stopEnabled;
    }

    [InjectOnClick(Resource.Id.PlayButton)]
    public void OnPlayButttonClicked(object source, EventArgs e) {
      EnsurePlaybackControllerAvailable();
      playbackController.Play();
    }

    [InjectOnClick(Resource.Id.StopButton)]
    public void OnStopButtonClicked(object source, EventArgs e) {
      EnsurePlaybackControllerAvailable();
      playbackController.Stop();
    }
  }
}
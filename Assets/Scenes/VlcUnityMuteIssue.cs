using System;
using System.Collections.Generic;
using LibVLCSharp;
using UnityEngine;
using UnityEngine.UI;

public class VlcUnityMuteIssue : MonoBehaviour
{
    private string videoUriA = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
    private string videoUriB = $"http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4";

    public RawImage rawImageA;
    public RawImage rawImageB;

    private RenderTexture renderTextureA;
    private RenderTexture renderTextureB;

    private LibVLC libVLC;
    private MediaPlayer mediaPlayerA;
    private MediaPlayer mediaPlayerB;
    private Texture2D texA;
    private Texture2D texB;

    private const int seekTimeDelta = 5000;

    private List<MediaPlayer> MediaPlayers => new List<MediaPlayer>()
    {
        mediaPlayerA,
        mediaPlayerB,
    };

    void Awake()
    {
        Core.Initialize(Application.dataPath);

        libVLC = new LibVLC(enableDebugLogs: true);

        mediaPlayerA = new MediaPlayer(libVLC);
        mediaPlayerB = new MediaPlayer(libVLC);

        mediaPlayerA.Media = new Media(new Uri(videoUriA));
        mediaPlayerB.Media = new Media(new Uri(videoUriB));

        mediaPlayerA.Play();
        mediaPlayerB.Play();

        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        // libVLC.Log += (s, e) => UnityEngine.Debug.Log(e.FormattedLog); // enable this for logs in the editor
    }

    public void ToggleMute(int i)
    {
        Debug.Log($"[VLC] Toggle Mute of player {i}");
        MediaPlayers[i].Mute = !MediaPlayers[i].Mute;
        Debug.Log($"[VLC] new mute of player {i}: {MediaPlayers[i].Mute}");
    }

    public void ToggleVolume(int i)
    {
        Debug.Log($"[VLC] Toggle Volume of player {i}");
        MediaPlayers[i].SetVolume(MediaPlayers[i].Volume > 0 ? 0 : 100);
        Debug.Log($"[VLC] new volume of player {i}: {MediaPlayers[i].Volume}");
    }

    public void SeekForward()
    {
        Debug.Log("[VLC] Seeking forward !");
        MediaPlayers.ForEach(it => it.SetTime(it.Time + seekTimeDelta));
    }

    public void SeekBackward()
    {
        Debug.Log("[VLC] Seeking backward !");
        MediaPlayers.ForEach(it => it.SetTime(it.Time - seekTimeDelta));
    }

    public void PlayPause()
    {
        Debug.Log ("[VLC] Toggling Play Pause !");
        MediaPlayers.ForEach(it =>
        {
            if (it.IsPlaying)
            {
                it.Pause();
            }
            else
            {
                it.Play();
            }
        });
    }

    public void Stop ()
    {
        Debug.Log ("[VLC] Stopping Player !");

        MediaPlayers.ForEach(it => it.Stop());
    }

    void Update()
    {
        UpdateVlcTexture(mediaPlayerA, ref texA);
        UpdateVlcTexture(mediaPlayerB, ref texB);

        UpdateRenderTexture(ref renderTextureA, texA, rawImageA);
        UpdateRenderTexture(ref renderTextureB, texB, rawImageB);
    }

    private void UpdateRenderTexture(ref RenderTexture renderTexture, Texture2D tex, RawImage rawImage)
    {
        if (renderTexture == null
            && tex != null)
        {
            renderTexture = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32); //Make a renderTexture the same size as vlctex
            rawImage.texture = renderTexture;
        }
        else if (renderTexture != null && tex != null)
        {
            var scale = new Vector2(1, -1);
            Graphics.Blit(tex, renderTexture, scale, Vector2.zero); //If you wanted to do post processing outside of VLC you could use a shader here.
        }
    }

    private void UpdateVlcTexture(MediaPlayer mediaPlayer, ref Texture2D tex)
    {
        if (!mediaPlayer.IsPlaying)
        {
            return;
        }

        if (tex == null)
        {
            // If received size is not null, it and scale the texture
            uint i_videoHeight = 0;
            uint i_videoWidth = 0;

            mediaPlayer.Size(0, ref i_videoWidth, ref i_videoHeight);
            var texptr = mediaPlayer.GetTexture(i_videoWidth, i_videoHeight, out bool updated);
            if (i_videoWidth != 0 && i_videoHeight != 0 && updated && texptr != IntPtr.Zero)
            {
                Debug.Log("Creating texture with height " + i_videoHeight + " and width " + i_videoWidth);
                tex = Texture2D.CreateExternalTexture((int)i_videoWidth,
                    (int)i_videoHeight,
                    TextureFormat.RGBA32,
                    false,
                    true,
                    texptr);
            }
        }
        else if (tex != null)
        {
            var texptr = mediaPlayer.GetTexture((uint)tex.width, (uint)tex.height, out bool updated);
            if (updated)
            {
                tex.UpdateExternalTexture(texptr);
            }
        }
    }

    private void OnDestroy()
    {
        mediaPlayerA.Dispose();
        mediaPlayerB.Dispose();
        libVLC.Dispose();

        Destroy(renderTextureA);
        Destroy(renderTextureB);
    }
}

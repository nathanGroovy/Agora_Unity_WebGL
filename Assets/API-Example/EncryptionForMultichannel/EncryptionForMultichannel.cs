﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using agora_gaming_rtc;
using agora_utilities;
using System.Text;

public class EncryptionForMultichannel : MonoBehaviour
{
    [SerializeField] private string APP_ID = "YOUR_APPID";

    [SerializeField] private string TOKEN_1 = "";

    [SerializeField] private string CHANNEL_NAME_1 = "YOUR_CHANNEL_NAME_1";

    [SerializeField] private string TOKEN_2 = "";
    public Text logText;
    private Logger logger;
    private IRtcEngine mRtcEngine = null;
    private const float Offset = 100;
    private AgoraChannel channel1 = null;

    public string SECRET = "";
    public string SALT = "";

    public ENCRYPTION_MODE ENCRYPTION_MODE = ENCRYPTION_MODE.AES_128_GCM2;

    // Use this for initialization
    void Start()
    {
        if (!CheckAppId())
        {
            return;
        }

        InitEngine();
        
        //channel setup.
        updateScreenShareNew();
    }

    public void updateScreenShareNew()
    {
        
    }

    void Update()
    {
        PermissionHelper.RequestMicrophontPermission();
        PermissionHelper.RequestCameraPermission();

    }

    bool CheckAppId()
    {
        logger = new Logger(logText);
        logger.DebugAssert(APP_ID.Length > 10, "Please fill in your appId in VideoCanvas!!!!!");
        return (APP_ID.Length > 10);
    }

    byte[] GetEncryptionSaltFromServer()
    {
        return Encoding.UTF8.GetBytes("EncryptionKdfSaltInBase64Strings");
    }
    
    public bool SetEncryption()
    {

        var config = new EncryptionConfig
        {
            encryptionMode = ENCRYPTION_MODE,
            encryptionKey = SECRET,
            encryptionKdfSalt = GetEncryptionSaltFromServer()
        };
        logger.UpdateLog(string.Format("encryption mode: {0} secret: {1} salt: {2}", ENCRYPTION_MODE, SECRET, config.encryptionKdfSalt.ToString()));
        try {
        channel1.EnableEncryption(true, config);
        } catch {
            return false;
        }
        return true;
    }
    


    //for starting/stopping a new screen share through IRtcEngine class.
    public void startNewScreenShare(bool audioEnabled)
    {
        mRtcEngine.StartNewScreenCaptureForWeb(1000, audioEnabled);
    }

    public void stopNewScreenShare()
    {
        mRtcEngine.StopNewScreenCaptureForWeb();
    }

    //for starting/stopping a screen share through IRtcEngine class.
    public void startScreenShare(bool audioEnabled)
    {
        mRtcEngine.StartScreenCaptureForWeb(audioEnabled);
    }

    public void stopScreenShare()
    {
        mRtcEngine.StopScreenCapture();
    }

    void InitEngine()
    {
        mRtcEngine = IRtcEngine.GetEngine(APP_ID);
        mRtcEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING);
        mRtcEngine.SetMultiChannelWant(true);
        mRtcEngine.EnableAudio();
        mRtcEngine.EnableVideo();
        mRtcEngine.EnableVideoObserver();
        mRtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

        channel1 = mRtcEngine.CreateChannel(CHANNEL_NAME_1);
        channel1.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

        channel1.ChannelOnJoinChannelSuccess = ChannelOnJoinChannelSuccessHandler;
        channel1.ChannelOnLeaveChannel = ChannelOnLeaveChannelHandler;
        channel1.ChannelOnUserJoined = ChannelOnUserJoinedHandler;
        channel1.ChannelOnError = ChannelOnErrorHandler;
        channel1.ChannelOnUserOffLine = ChannelOnUserOfflineHandler;
        channel1.ChannelOnScreenShareStarted = screenShareStartedHandler_MC;
        channel1.ChannelOnScreenShareStopped = screenShareStoppedHandler_MC;
        channel1.ChannelOnScreenShareCanceled = screenShareCanceledHandler_MC;
    }

    public void JoinChannel()
    {
        SetEncryption();
        channel1.JoinChannel(TOKEN_1, "", 0, new ChannelMediaOptions(true, true, true, true));
    }

    public void LeaveChannel()
    {
        channel1.LeaveChannel();
    }

    void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit");
        if (mRtcEngine != null)
        {

            mRtcEngine.DisableVideoObserver();
            IRtcEngine.Destroy();
        }
    }

    void screenShareStartedHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("onScreenShareStarted channelId: {0}, uid: {1}, elapsed: {2}", channelId, uid,
            elapsed));
    }

    void screenShareStoppedHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("onScreenShareStopped channelId: {0}, uid: {1}, elapsed: {2}", channelId, uid,
            elapsed));
    }

    void screenShareCanceledHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("onScreenShareCanceled channelId: {0}, uid: {1}, elapsed: {2}", channelId, uid,
            elapsed));
    }

    void screenShareStartedHandler_MC(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("onScreenShareStartedMC channelId: {0}, uid: {1}, elapsed: {2}", channelId, uid,
            elapsed));
    }

    void screenShareStoppedHandler_MC(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("onScreenShareStoppedMC channelId: {0}, uid: {1}, elapsed: {2}", channelId, uid,
            elapsed));
    }

    void screenShareCanceledHandler_MC(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("onScreenShareCanceledMC channelId: {0}, uid: {1}, elapsed: {2}", channelId, uid,
            elapsed));
    }

    void ChannelOnJoinChannelSuccessHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("sdk version: ${0}", IRtcEngine.GetSdkVersion()));
        logger.UpdateLog(string.Format("ChannelOnJoinChannelSuccess channelId: {0}, uid: {1}, elapsed: {2}", CHANNEL_NAME_1, uid,
            elapsed));
        makeVideoView(CHANNEL_NAME_1, 0);
    }

    void ChannelOnLeaveChannelHandler(string channelId, RtcStats rtcStats)
    {
        logger.UpdateLog(string.Format("OnLeaveChannelHandler channelId: {0}", channelId));
    }

    void ChannelOnErrorHandler(string channelId, int err, string message)
    {
        logger.UpdateLog(string.Format("Channel2OnErrorHandler channelId: {0}, err: {1}, message: {2}", channelId, err,
            message));
    }

    void ChannelOnUserJoinedHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("Channel1OnUserJoinedHandler channelId: {0} uid: ${1} elapsed: ${2}", CHANNEL_NAME_1,
            uid, elapsed));
        makeVideoView(CHANNEL_NAME_1, uid);
    }

    void ChannelOnUserOfflineHandler(string channelId, uint uid, USER_OFFLINE_REASON reason)
    {
        logger.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid, (int)reason));
        DestroyVideoView(CHANNEL_NAME_1, uid);
    }

    public void RespawnLocal(string channelName)
    {
        GameObject go = GameObject.Find(channelName + "_0");
        if (go != null)
        {
            go.name = "Destroying";
            Destroy(go);
            makeVideoView(channelName, 0);
        }
    }

    public void RespawnRemote()
    {
        if (LastRemote != null)
        {
            string[] strs = LastRemote.name.Split('_');
            string channel = strs[0];
            uint uid = uint.Parse(strs[1]);
            LastRemote.name = "_Destroyer";
            Destroy(LastRemote);
            Debug.LogWarningFormat("Remaking video surface for  uid:{0} channel:{1}", uid, channel);
            makeVideoView(channel, uid);
        }
    }

    GameObject LastRemote = null;

    private void makeVideoView(string channelId, uint uid)
    {
        string objName = channelId + "_" + uid.ToString();
        GameObject go = GameObject.Find(objName);
        if (!ReferenceEquals(go, null))
        {
            return; // reuse
        }


        // create a GameObject and assign to this new user
        VideoSurface videoSurface = makeImageSurface(objName);
        if (!ReferenceEquals(videoSurface, null))
        {
            // configure videoSurface
            videoSurface.SetForMultiChannelUser(channelId, uid);
            videoSurface.SetEnable(true);
            videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);

            // make the object draggable
            videoSurface.gameObject.AddComponent<UIElementDragger>();

            if (uid != 0)
            {
                LastRemote = videoSurface.gameObject;
            }
        }
    }

    // Video TYPE 2: RawImage
    public VideoSurface makeImageSurface(string goName)
    {
        GameObject go = new GameObject();

        if (go == null)
        {
            return null;
        }

        go.name = goName;
        // make the object draggable
        go.AddComponent<UIElementDrag>();
        // to be renderered onto
        go.AddComponent<RawImage>();

        GameObject canvas = GameObject.Find("VideoCanvas");
        if (canvas != null)
        {
            go.transform.SetParent(canvas.transform);
            Debug.Log("add video view");
        }
        else
        {
            Debug.Log("Canvas is null video view");
        }

        // set up transform
        go.transform.Rotate(0f, 0.0f, 180.0f);
        float xPos = Random.Range(Offset - Screen.width / 2f, Screen.width / 2f - Offset);
        float yPos = Random.Range(Offset, Screen.height / 2f - Offset);
        Debug.Log("position x " + xPos + " y: " + yPos);
        go.transform.localPosition = new Vector3(xPos, yPos, 0f);
        go.transform.localScale = new Vector3(1.5f, 1f, 1f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }

    private void DestroyVideoView(string channelId, uint uid)
    {
        string objName = channelId + "_" + uid.ToString();
        GameObject go = GameObject.Find(objName);
        if (!ReferenceEquals(go, null))
        {
            Object.Destroy(go);
        }
    }
}
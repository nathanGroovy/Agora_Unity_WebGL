using System.Collections;
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

    public Text logText;
    private Logger logger;
    private IRtcEngine mRtcEngine = null;
    private AgoraChannel channel1 = null;
    private const float Offset = 100;

    protected Dictionary<uint, VideoSurface> UserVideoDict = new Dictionary<uint, VideoSurface>();
    private List<GameObject> remoteUserDisplays = new List<GameObject>();

    public EncryptionConfig myConfig;

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

        //encryption setup.
        SetEncryption();
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

    void SetEncryption()
    {
        var config = new EncryptionConfig
        {
            encryptionMode = ENCRYPTION_MODE,
            encryptionKey = SECRET,
            encryptionKdfSalt = Encoding.UTF8.GetBytes(SALT)
        };
        foreach(var b in config.encryptionKdfSalt){
            Debug.Log(b.ToString());
        }
        logger.UpdateLog(string.Format("encryption mode: {0} secret: {1}", ENCRYPTION_MODE, SECRET));
        mRtcEngine.EnableEncryption2(true, config);
    }

    void InitEngine()
    {
        mRtcEngine = IRtcEngine.GetEngine(APP_ID);
        mRtcEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING);
        // If you want to user Multi Channel Video, please call "SetMultiChannleWant to true"
        mRtcEngine.SetMultiChannelWant(true);
        mRtcEngine.EnableAudio();
        mRtcEngine.EnableVideo();
        mRtcEngine.EnableVideoObserver();
        mRtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

        channel1 = mRtcEngine.CreateChannel(CHANNEL_NAME_1);
        channel1.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

        channel1.ChannelOnJoinChannelSuccess = Channel1OnJoinChannelSuccessHandler;
        channel1.ChannelOnLeaveChannel = Channel1OnLeaveChannelHandler;
        channel1.ChannelOnUserJoined = Channel1OnUserJoinedHandler;
        channel1.ChannelOnError = Channel1OnErrorHandler;
        channel1.ChannelOnUserOffLine = ChannelOnUserOfflineHandler;
        channel1.ChannelOnScreenShareStarted = screenShareStartedHandler_MC;
        channel1.ChannelOnScreenShareStopped = screenShareStoppedHandler_MC;
        channel1.ChannelOnScreenShareCanceled = screenShareCanceledHandler_MC;
        channel1.ChannelOnVideoSizeChanged = onVideoSizeChanged_MCHandler;

    }

    public void JoinChannel1()
    {
        channel1.JoinChannel(TOKEN_1, "", 0, new ChannelMediaOptions(true, true));
        
    }

    public void LeaveChannel1()
    {
        channel1.LeaveChannel();
    }

    void JoinChannel()
    {
        mRtcEngine.JoinChannel(TOKEN_1, CHANNEL_NAME_1, "", 0, new ChannelMediaOptions(true, true, true, true));
    }


    void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit");
        if (mRtcEngine != null)
        {
            channel1.LeaveChannel();
            channel1.ReleaseChannel();

            mRtcEngine.DisableVideoObserver();
            IRtcEngine.Destroy();
        }
    }

    float EnforcingViewLength = 180f;
    void onVideoSizeChanged_MCHandler(string channelID, uint uid, int width, int height, int rotation)
    {

        logger.UpdateLog(string.Format("channelOnVideoSizeChanged channelID: {3}, uid: {0}, width: {1}, height: {2}", uid,
            width, height, channelID));
        if (UserVideoDict.ContainsKey(uid))
        {
            GameObject go = UserVideoDict[uid].gameObject;
            Vector2 v2 = new Vector2(width, height);
            RawImage image = go.GetComponent<RawImage>();
            v2 = AgoraUIUtils.GetScaledDimension(width, height, EnforcingViewLength);

            if (rotation == 90 || rotation == 270)
            {
                v2 = new Vector2(v2.y, v2.x);
            }
            image.rectTransform.sizeDelta = v2;
        }
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

    void Channel1OnJoinChannelSuccessHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("sdk version: ${0}", IRtcEngine.GetSdkVersion()));
        logger.UpdateLog(string.Format("onJoinChannelSuccess channelId: {0}, uid: {1}, elapsed: {2}", channelId, uid,
            elapsed));
        makeVideoView(channelId, 0);
    }

    void Channel2OnJoinChannelSuccessHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("sdk version: ${0}", IRtcEngine.GetSdkVersion()));
        logger.UpdateLog(string.Format("onJoinChannelSuccess channelId: {0}, uid: {1}, elapsed: {2}", channelId, uid,
            elapsed));
        makeVideoView(channelId, 0);
    }

    void EngineOnJoinChannelSuccessHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("sdk version: ${0}", IRtcEngine.GetSdkVersion()));
        logger.UpdateLog(string.Format("EngineOnJoinChannelSuccess channelId: {0}, uid: {1}, elapsed: {2}", CHANNEL_NAME_1, uid,
            elapsed));
        makeVideoView(channelId, 0);
    }

    void Channel1OnLeaveChannelHandler(string channelId, RtcStats rtcStats)
    {
        logger.UpdateLog(string.Format("Channel1OnLeaveChannelHandler channelId: {0}", channelId));

    }

    void Channel2OnLeaveChannelHandler(string channelId, RtcStats rtcStats)
    {
        logger.UpdateLog(string.Format("Channel2OnLeaveChannelHandler channelId: {0}", channelId));
    }

    void EngineOnLeaveChannelHandler(RtcStats rtcStats)
    {
        logger.UpdateLog(string.Format("EngineOnLeaveChannelHandler channelId: {0}", CHANNEL_NAME_1));
    }

    void Channel1OnErrorHandler(string channelId, int err, string message)
    {
        logger.UpdateLog(string.Format("Channel1OnErrorHandler channelId: {0}, err: {1}, message: {2}", channelId, err,
            message));
    }

    void Channel2OnErrorHandler(string channelId, int err, string message)
    {
        logger.UpdateLog(string.Format("Channel2OnErrorHandler channelId: {0}, err: {1}, message: {2}", channelId, err,
            message));
    }

    void EngineOnErrorHandler(int err, string message)
    {
        logger.UpdateLog(string.Format("EngineOnErrorHandler channelId: {0}, err: {1}, message: {2}", CHANNEL_NAME_1, err,
            message));
    }


    void Channel1OnUserJoinedHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("Channel1OnUserJoinedHandler channelId: {0} uid: ${1} elapsed: ${2}", channelId,
            uid, elapsed));
        makeVideoView(channelId, uid, true);
    }

    void EngineOnUserJoinedHandler(uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("EngineOnUserJoinedHandler channelId: {0} uid: ${1} elapsed: ${2}", CHANNEL_NAME_1,
            uid, elapsed));
        makeVideoView(CHANNEL_NAME_1, uid);
    }

    void Channel2OnUserJoinedHandler(string channelId, uint uid, int elapsed)
    {
        logger.UpdateLog(string.Format("Channel2OnUserJoinedHandler channelId: {0} uid: ${1} elapsed: ${2}", channelId,
            uid, elapsed));
        makeVideoView(channelId, uid, true);
    }

    void ChannelOnUserOfflineHandler(string channelId, uint uid, USER_OFFLINE_REASON reason)
    {
        logger.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid, (int)reason));
        DestroyVideoView(channelId, uid);
    }

    void EngineOnUserOfflineHandler(uint uid, USER_OFFLINE_REASON reason)
    {
        logger.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid, (int)reason));
        DestroyVideoView(CHANNEL_NAME_1, uid);
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
            remoteUserDisplays.Remove(LastRemote);
            makeVideoView(channel, uid, true);
        }
    }

    GameObject LastRemote = null;

    private void makeVideoView(string channelId, uint uid, bool remote = false)
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

            if (remote)
            {
                Debug.Log("is remote " + uid.ToString() + remote.ToString());
                remoteUserDisplays.Add(videoSurface.gameObject);
                UserVideoDict[uid] = videoSurface;
            }
            else
            {
                Vector2 v2 = AgoraUIUtils.GetScaledDimension(640, 360, EnforcingViewLength);
                videoSurface.GetComponent<RawImage>().rectTransform.sizeDelta = v2;
            }
        }
    }

    public VideoSurface makeImageSurface(string goName)
    {
        GameObject go = new GameObject();

        if (go == null)
        {
            return null;
        }

        go.name = goName;

        // to be renderered onto
        go.AddComponent<RawImage>();

        // make the object draggable
        go.AddComponent<UIElementDragger>();
        GameObject canvas = GameObject.Find("VideoCanvas");
        if (canvas != null)
        {
            go.transform.SetParent(canvas.transform);
        }
        // set up transform
        go.transform.Rotate(0f, 0.0f, 180.0f);
        float xPos = Random.Range(-Screen.width / 5f, Screen.width / 5f);
        float yPos = Random.Range(-Screen.height / 5f, Screen.height / 5f);
        go.transform.localPosition = new Vector3(xPos, yPos, 0f);

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

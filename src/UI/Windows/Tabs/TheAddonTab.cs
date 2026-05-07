using UnityEngine;
using System;

namespace MalumMenu;

public class ExploitsTab : ITab
{
    public string name => "TheAddon";

    private bool instantGameEnd = false;
    private float lastCrashMessageTime = 0f;
    private float lastMeetingCallTime = 0f;

    private const float CHAT_DELAY = 3.1f;
    private const float MEETING_CALL_DELAY = 0.5f;
    private const float CRASH_GRACE_PERIOD = 3f;
    private const float CRASH_NOTIFICATION_DELAY = 0.5f;

    private bool crashMessagingActive = false;
    private bool wasInMainMenu = true;
    private bool isCrashActive = false;
    private bool hasImpostorsDetected = false;
    private bool crashDetected = false;

    private float crashDisableTime = 0f;
    private float crashDetectionTime = 0f;

    public void Draw()
    {
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(15);

        UpdateServerCrash();

        GUILayout.EndVertical();
    }

    private void UpdateServerCrash()
    {
        bool hasImpostors = HasImpostorsInGame();
        bool wasActive = isCrashActive;

        if (DetectLoadingScreen() && Utils.isLobby && instantGameEnd && !crashDetected)
        {
            crashDetected = true;
            crashDetectionTime = Time.time;
        }

        if (crashDetected &&
            PlayerControl.LocalPlayer != null &&
            (Time.time - crashDetectionTime) >= CRASH_NOTIFICATION_DELAY)
        {
            SendCrashNotification();

            crashDetected = false;
            crashMessagingActive = true;
            lastCrashMessageTime = Time.time;
        }

        if (crashMessagingActive)
        {
            try
            {
                if (Utils.isInGame || Utils.isLobby)
                {
                    if (PlayerControl.LocalPlayer != null &&
                        AmongUsClient.Instance != null &&
                        (Time.time - lastCrashMessageTime) >= CHAT_DELAY)
                    {
                        PlayerControl.LocalPlayer.RpcSendChat("Crashed by discord,gg/addon - Join for hacks and more");
                        lastCrashMessageTime = Time.time;
                    }
                }
                else if (!wasInMainMenu)
                {
                    crashMessagingActive = false;
                    lastCrashMessageTime = 0f;
                    wasInMainMenu = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        if ((Utils.isInGame || Utils.isLobby) && wasInMainMenu)
        {
            wasInMainMenu = false;
        }

        if (crashDetected)
        {
            isCrashActive = false;
            return;
        }

        isCrashActive = instantGameEnd && !hasImpostors;

        if (wasActive && !isCrashActive && hasImpostors)
        {
            hasImpostorsDetected = true;
            crashDisableTime = Time.time;
        }

        if (isCrashActive ||
            (hasImpostorsDetected && (Time.time - crashDisableTime) < CRASH_GRACE_PERIOD))
        {
            if ((Time.time - lastMeetingCallTime) >= MEETING_CALL_DELAY)
            {
                SendCrashMessage();
            }
        }
        else if (hasImpostorsDetected)
        {
            hasImpostorsDetected = false;
        }
    }

    private void SendCrashNotification()
    {
        try
        {
            if (PlayerControl.LocalPlayer != null && AmongUsClient.Instance != null)
            {
                PlayerControl.LocalPlayer.RpcSendChat("Crashed by discord,gg/addon - Join for hacks and more");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private bool HasImpostorsInGame()
    {
        try
        {
            if (GameData.Instance == null)
                return false;

            foreach (var playerInfo in GameData.Instance.AllPlayers)
            {
                if (playerInfo?.Role != null &&
                    playerInfo.Role.TeamType == RoleTeamTypes.Impostor)
                {
                    return true;
                }
            }
        }
        catch { }

        return false;
    }

    private bool DetectLoadingScreen()
    {
        try
        {
            var progressBar = UnityEngine.Object.FindObjectOfType<ProgressBar>();

            return progressBar != null &&
                   progressBar.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void SendCrashMessage()
    {
        try
        {
            if (PlayerControl.LocalPlayer == null ||
                AmongUsClient.Instance == null ||
                !Utils.isPlayer ||
                !Utils.isInGame)
            {
                return;
            }

            if (Utils.isHost)
            {
                MeetingRoomManager.Instance.AssignSelf(PlayerControl.LocalPlayer, null);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(PlayerControl.LocalPlayer);
                PlayerControl.LocalPlayer.RpcStartMeeting(null);
            }
            else
            {
                PlayerControl.LocalPlayer.CmdReportDeadBody(null);
            }

            lastMeetingCallTime = Time.time;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private void DrawGeneral()
    {
        GUILayout.Label("Made by NyveX an 4Pierce\n");
        GUILayout.Space(5);

        bool hasImpostors = HasImpostorsInGame();

        if (hasImpostors)
        {
            GUILayout.Label(" ServerCrash only in Lobby", GUILayout.Height(25));
            GUILayout.Space(5);
        }

        GUI.enabled = !hasImpostors;
        instantGameEnd = GUILayout.Toggle(instantGameEnd, " ServerCrash on GameStart");
        GUI.enabled = true;
    }
}
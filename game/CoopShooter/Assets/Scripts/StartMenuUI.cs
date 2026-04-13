using System;
using UnityEngine;

[Obsolete("Use MainMenuController. This wrapper only keeps older scene button references working.")]
public class StartMenuUI : MonoBehaviour
{
    private MainMenuController controller;

    private MainMenuController Controller
    {
        get
        {
            if (controller == null)
                controller = GetComponent<MainMenuController>();

            return controller;
        }
    }

    public async void Host()
    {
        if (Controller != null)
            await Controller.HostPartyAsync();
    }

    public async void Join()
    {
        Debug.Log("[StartMenuUI] Join-code flow was removed. Use Friends notifications to accept lobby invites.");
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public void StartGame()
    {
        Controller?.StartGameForParty();
    }

    public void Disconnect()
    {
        Controller?.DisconnectParty();
    }
}

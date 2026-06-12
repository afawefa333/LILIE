using UnityEngine;

public class PlayerDebugOverlay : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    public PlayerController Player
    {
        get => player;
        set => player = value;
    }

    private void OnGUI()
    {
        if (player == null)
        {
            return;
        }

        GUI.Box(new Rect(12f, 12f, 320f, 150f), "Player Debug");
        GUI.Label(new Rect(24f, 38f, 250f, 20f), $"Speed: {player.Speed:0.00}");
        GUI.Label(new Rect(24f, 58f, 250f, 20f), $"HorizontalInput: {player.HorizontalInput:0.00}");
        GUI.Label(new Rect(24f, 78f, 250f, 20f), $"IsGrounded: {player.IsGrounded}");
        GUI.Label(new Rect(24f, 98f, 250f, 20f), $"VerticalVelocity: {player.VerticalVelocity:0.00}");
        GUI.Label(new Rect(24f, 118f, 280f, 20f), $"Current Animation State: {player.CurrentAnimationState}");
        GUI.Label(new Rect(24f, 138f, 250f, 20f), $"Application Focused: {Application.isFocused}");
    }
}

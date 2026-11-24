using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace RSPP
{
    /// <summary>
    /// Short controller vibrations. Safe to call when no headset is connected:
    /// the call simply does nothing.
    /// </summary>
    public static class Haptics
    {
        public static HapticImpulsePlayer Left;
        public static HapticImpulsePlayer Right;

        public static void PulseLeft(float amplitude, float duration)
        {
            Send(Left, amplitude, duration);
        }

        public static void PulseRight(float amplitude, float duration)
        {
            Send(Right, amplitude, duration);
        }

        public static void PulseBoth(float amplitude, float duration)
        {
            Send(Left, amplitude, duration);
            Send(Right, amplitude, duration);
        }

        private static void Send(HapticImpulsePlayer player, float amplitude, float duration)
        {
            if (player != null && player.isActiveAndEnabled)
                player.SendHapticImpulse(amplitude, duration);
        }
    }
}

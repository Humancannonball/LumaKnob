namespace LumaKnob.Engine.Platform;

public readonly record struct SystemInputRegistration(
    bool EmergencyRestoreRegistered,
    int EmergencyRestoreError,
    bool BrightnessKeysListening,
    int BrightnessKeysError);

public interface ISystemInputService : IDisposable
{
    event EventHandler? EmergencyRestorePressed;

    event EventHandler? BrightnessIncreasePressed;

    event EventHandler? BrightnessDecreasePressed;

    SystemInputRegistration Start();
}

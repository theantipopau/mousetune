namespace MouseTune.Services;

public interface IPointerSettingsNative
{
    PointerSettings Read();
    void Apply(PointerSettings settings);
}

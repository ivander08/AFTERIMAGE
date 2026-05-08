using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class SettingsController : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider musicSlider;

    [Header("Shadow Toggle")]
    public Button shadowButton;
    public TMPro.TextMeshProUGUI shadowButtonText;

    private bool _shadowsEnabled = false;
    private float _originalShadowDistance;
    private UniversalRenderPipelineAsset _urpAsset;

    private void Awake()
    {
        _urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        _originalShadowDistance = (_urpAsset != null && _urpAsset.shadowDistance > 0f)
            ? _urpAsset.shadowDistance : 50f;

        // Wire listeners FIRST
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        shadowButton.onClick.AddListener(OnShadowToggle);
    }

    // ADD THIS
     private void Start()
    {
        // MODIFIED: Changed default fallback from 0.5f to 1.0f
        masterSlider.value = PlayerPrefs.GetFloat("Pref_MasterVol", 1f);
        sfxSlider.value    = PlayerPrefs.GetFloat("Pref_SFXVol",    1f);
        musicSlider.value  = PlayerPrefs.GetFloat("Pref_MusicVol",  1f);

        _shadowsEnabled = PlayerPrefs.GetInt("Pref_Shadows", 0) == 1;
        ApplyShadows(_shadowsEnabled);
        RefreshShadowLabel();
    }

    private void OnMasterChanged(float value)
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMasterVolume(value);
        PlayerPrefs.SetFloat("Pref_MasterVol", value);
    }

    private void OnSFXChanged(float value)
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetSFXVolume(value);
        PlayerPrefs.SetFloat("Pref_SFXVol", value);
    }

    private void OnMusicChanged(float value)
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMusicVolume(value);
        PlayerPrefs.SetFloat("Pref_MusicVol", value);
    }

    private void OnShadowToggle()
    {
        _shadowsEnabled = !_shadowsEnabled;
        ApplyShadows(_shadowsEnabled);
        RefreshShadowLabel();
        PlayerPrefs.SetInt("Pref_Shadows", _shadowsEnabled ? 1 : 0);
    }

    private void ApplyShadows(bool enabled)
    {
        if (_urpAsset == null) return;
        _urpAsset.shadowDistance = enabled ? _originalShadowDistance : 0f;
    }

    private void RefreshShadowLabel()
    {
        if (shadowButtonText != null)
            shadowButtonText.text = _shadowsEnabled ? "Shadows: ON" : "Shadows: OFF";
    }
}
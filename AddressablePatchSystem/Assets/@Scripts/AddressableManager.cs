using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.AddressableAssets.ResourceLocators;
using System.Linq;

public class AddressableManager : MonoBehaviour
{
    enum PopupType
    {
        None,
        PATCH,
        DOWNLOAD,
        SUCCESS,
        FAIL,
    }

    [SerializeField] private Button _patchButton;
    [SerializeField] private Button _successPopupCloseButton;
    [SerializeField] private Button _failPopupCloseButton;
    [SerializeField] private GameObject _patchPopup;
    [SerializeField] private GameObject _downloadPopup;
    [SerializeField] private GameObject _successPopup;
    [SerializeField] private GameObject _failPopup;

    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _totalSizeText;

    [SerializeField] private AssetLabelReference prefabLabel;
    [SerializeField] private AssetLabelReference spriteLabel;

    private long patchSize;
    private Dictionary<string, long> patchMap = new Dictionary<string, long>();

    private void Awake()
    {
        _patchButton.onClick.AddListener(() => Button_Download());
        _successPopupCloseButton.onClick.AddListener(() => SceneManager.LoadScene("MainScene"));
        _failPopupCloseButton.onClick.AddListener(() => PopupShow(PopupType.None));

        StartCoroutine(InitAddressable());
        StartCoroutine(CheckUpdateFiles());
    }

    private void PopupShow(PopupType type = PopupType.None)
    {
        _downloadPopup.SetActive(false);
        _successPopup.SetActive(false);
        _failPopup.SetActive(false);
        _patchPopup.SetActive(false);

        switch (type)
        {
            case PopupType.DOWNLOAD:
                _downloadPopup.SetActive(true);
                break;
            case PopupType.FAIL:
                _failPopup.SetActive(true);
                break;
            case PopupType.PATCH:
                _patchPopup.SetActive(true);
                break;
            case PopupType.SUCCESS:
                _successPopup.SetActive(true);
                break;
            case PopupType.None:
                break;
        }
    }

    IEnumerator InitAddressable()
    {
        var init = Addressables.InitializeAsync();
        yield return init;
    }
    #region Patch Check
    IEnumerator CheckUpdateFiles()
    {
        List<string> labels = new List<string> { prefabLabel.labelString, spriteLabel.labelString };
        patchSize = default;
        foreach(string label in labels)
        {
            var handle = Addressables.GetDownloadSizeAsync(label);
            yield return handle;
            patchSize += handle.Result;
        }

        if(patchSize > decimal.Zero)
        {
            PopupShow(PopupType.PATCH);
            _totalSizeText.text = GetFileSize(patchSize);
        }
        else
        {
            Debug.LogError("Already Patch");
            SceneManager.LoadScene("MainScene");
        }
    }

    private string GetFileSize(long byteCnt)
    {
        string size = "o Bytes";

        if (byteCnt >= 1073741824.0)
        {
            size = string.Format("{0:##.##}", byteCnt / 1073741824.0) + " GB";
        }
        else if (byteCnt >= 1048576.0)
        {
            size = string.Format("{0:##.##}", byteCnt / 1048576.0) + " MB";
        }
        else if (byteCnt >= 1024.0)
        {
            size = string.Format("{0:##.##}", byteCnt / 1024.0) + " KB";
        }
        else if (byteCnt > 0 && byteCnt < 1024.0)
        {
            size = byteCnt.ToString() + " Bytes";
        }

        return size;
    }
    #endregion

    #region Download
    public void Button_Download()
    {
        PopupShow(PopupType.DOWNLOAD);
        StartCoroutine(PatchFiles());
    }

    IEnumerator PatchFiles()
    {
        List<string> labels = new List<string> { prefabLabel.labelString, spriteLabel.labelString };
        
        foreach(string label in labels)
        {
            var handle = Addressables.GetDownloadSizeAsync(label);

            yield return handle;
            
            if(handle.Result != decimal.Zero)
            {
                StartCoroutine(DownloadLabel(label));
            }
        }

        yield return CheckDownLoad();
    }

    IEnumerator DownloadLabel(string label)
    {
        patchMap.Add(label, 0);

        var handle = Addressables.DownloadDependenciesAsync(label, false);

        while(!handle.IsDone)
        {
            patchMap[label] = handle.GetDownloadStatus().DownloadedBytes;
            yield return new WaitForEndOfFrame();
        }

        patchMap[label] = handle.GetDownloadStatus().TotalBytes;
        Addressables.Release(handle);
    }
    IEnumerator CheckDownLoad()
    {
        var total = 0f;
        _progressText.text = "0 %";

        while (true)
        {
            total += patchMap.Sum(tmp => tmp.Value);

            _slider.value = total / patchSize;
            _progressText.text = (int)(_slider.value * 100) + " %";

            if (total == patchSize)
            {
                PopupShow(PopupType.SUCCESS);
                break;
            }

            total = 0f;
            yield return new WaitForEndOfFrame();
        }
    }

    #endregion
}


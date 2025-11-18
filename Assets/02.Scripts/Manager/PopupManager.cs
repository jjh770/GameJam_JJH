using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Popup UI")]
    [SerializeField] private GameObject _popupPanel;
    [SerializeField] private GameObject _contentPanel;        // 일반 팝업 컨텐츠
    [SerializeField] private GameObject _content2Panel;       // 매뉴얼 팝업 컨텐츠
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _manualButton;
    [SerializeField] private TextMeshProUGUI _confirmButtonText;
    [SerializeField] private TextMeshProUGUI _cancelButtonText;

    [Header("Content2 Panel Buttons")]
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _closeButton;

    [Header("Content2 Panel Pages")]
    [SerializeField] private GameObject[] _manualPages; // 매뉴얼 페이지들
    private int _currentPageIndex = 0;

    [Header("Animation Settings")]
    [SerializeField] private float _fadeDuration = 0.3f;
    [SerializeField] private float _scaleDuration = 0.3f;
    [SerializeField] private Ease _scaleEase = Ease.OutBack;
    [SerializeField] private float _pageTransitionDuration = 0.3f;

    [Header("Background")]
    [SerializeField] private Image _backgroundDimmer; // 배경 어두운 효과

    private Action _onConfirmCallback;
    private Action _onCancelCallback;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 초기에는 팝업 숨김
        _popupPanel.SetActive(false);
        _contentPanel.SetActive(false);
        _content2Panel.SetActive(false);

        // 버튼 이벤트 연결
        _confirmButton.onClick.AddListener(OnConfirmClicked);
        _cancelButton.onClick.AddListener(OnCancelClicked);
        _manualButton.onClick.AddListener(ShowManualPopup);

        // Content2Panel 버튼 연결
        _leftButton.onClick.AddListener(OnLeftButtonClicked);
        _rightButton.onClick.AddListener(OnRightButtonClicked);
        _closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    // 매뉴얼 팝업 (Content2Panel 표시)
    public void ShowManualPopup()
    {
        _currentPageIndex = 0;
        SwitchToPanel(_content2Panel);
        UpdateManualPage();
    }

    // 배팅 금액 부족 팝업
    public void ShowInsufficientFundsPopup()
    {
        SoundManager.Instance.PlaySFX(SoundManager.SFXType.Error);

        ShowPopup(
            "배팅 금액 부족",
            "배팅 금액이 부족합니다!\n배팅 금액을 낮춰주세요.",
            "O",
            null,
            () => { HidePopup(); },
            null
        );
    }
    // 게임 승리 팝업
    public void ShowGameWinPopup(Action onRestart = null)
    {
        ShowPopup(
            "Victory!",
            $"게임을 승리하셨습니다!\n이번 게임 총액 : {MoneyManager.Instance.CheckTotalMoney()}",
            "O",
            "X",
            () => {
                HidePopup();
                onRestart?.Invoke();
            },
            () => {
                HidePopup();
                // 게임 종료 로직
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                    Application.Quit();
            #endif
            }
        );
    }
    // 게임 오버 팝업
    public void ShowGameOverPopup(Action onRestart = null)
    {
        ShowPopup(
            "Game Over",
            "소지금이 부족합니다!\n게임을 재시작하시겠습니까?",
            "O",
            "X",
            () => {
                HidePopup();
                onRestart?.Invoke();
            },
            () => {
                HidePopup();
                // 게임 종료 로직
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                    Application.Quit();
            #endif
            }
        );
    }

    // 범용 팝업 표시
    public void ShowPopup(
        string title,
        string message,
        string confirmText,
        string cancelText = null,
        Action onConfirm = null,
        Action onCancel = null
    )
    {
        // 텍스트 설정
        _titleText.text = title;
        _messageText.text = message;
        _confirmButtonText.text = confirmText;

        // 콜백 저장
        _onConfirmCallback = onConfirm;
        _onCancelCallback = onCancel;

        // 취소 버튼 표시 여부
        if (string.IsNullOrEmpty(cancelText))
        {
            _cancelButton.gameObject.SetActive(false);
        }
        else
        {
            _cancelButton.gameObject.SetActive(true);
            _cancelButtonText.text = cancelText;
        }

        // ContentPanel 활성화, Content2Panel 비활성화
        _contentPanel.SetActive(true);
        _content2Panel.SetActive(false);

        // 팝업 표시 애니메이션
        _popupPanel.SetActive(true);
        ShowPopupAnimation();
    }

    // 패널 전환 (ContentPanel <-> Content2Panel)
    private void SwitchToPanel(GameObject targetPanel)
    {
        // 모든 패널 비활성화
        _contentPanel.SetActive(false);
        _content2Panel.SetActive(false);

        // 타겟 패널만 활성화
        targetPanel.SetActive(true);

        // 팝업이 이미 열려있지 않으면 열기
        if (!_popupPanel.activeSelf)
        {
            _popupPanel.SetActive(true);
            ShowPopupAnimation();
        }
    }

    // 매뉴얼 페이지 업데이트
    private void UpdateManualPage()
    {
        if (_manualPages == null || _manualPages.Length == 0)
            return;

        // 모든 페이지 비활성화
        for (int i = 0; i < _manualPages.Length; i++)
        {
            _manualPages[i].SetActive(false);
        }

        // 현재 페이지만 활성화
        _manualPages[_currentPageIndex].SetActive(true);

        // 페이지 전환 애니메이션
        _manualPages[_currentPageIndex].transform.localScale = Vector3.zero;
        _manualPages[_currentPageIndex].transform.DOScale(Vector3.one, _pageTransitionDuration)
            .SetEase(Ease.OutBack);

        // 버튼 상태 업데이트
        UpdateNavigationButtons();
    }

    // 네비게이션 버튼 상태 업데이트
    private void UpdateNavigationButtons()
    {
        if (_manualPages == null || _manualPages.Length == 0)
            return;

        // 첫 페이지면 왼쪽 버튼 비활성화
        _leftButton.interactable = _currentPageIndex > 0;

        // 마지막 페이지면 오른쪽 버튼 비활성화
        _rightButton.interactable = _currentPageIndex < _manualPages.Length - 1;
    }

    // 좌측 버튼 클릭
    private void OnLeftButtonClicked()
    {
        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            UpdateManualPage();
        }
    }

    // 우측 버튼 클릭
    private void OnRightButtonClicked()
    {
        if (_manualPages != null && _currentPageIndex < _manualPages.Length - 1)
        {
            _currentPageIndex++;
            UpdateManualPage();
        }
    }

    // 닫기 버튼 클릭
    private void OnCloseButtonClicked()
    {
        HidePopup();
    }


    private void ShowPopupAnimation()
    {
        // 초기 상태 설정
        _popupPanel.transform.localScale = Vector3.zero;

        if (_backgroundDimmer != null)
        {
            Color dimColor = _backgroundDimmer.color;
            dimColor.a = 0f;
            _backgroundDimmer.color = dimColor;
        }

        // 배경 페이드 인
        if (_backgroundDimmer != null)
        {
            _backgroundDimmer.DOFade(0.7f, _fadeDuration);
        }

        // 팝업 스케일 애니메이션
        _popupPanel.transform.DOScale(Vector3.one, _scaleDuration)
            .SetEase(_scaleEase);
    }

    private void HidePopup()
    {
        // 배경 페이드 아웃
        if (_backgroundDimmer != null)
        {
            _backgroundDimmer.DOFade(0f, _fadeDuration);
        }

        // 팝업 스케일 애니메이션
        _popupPanel.transform.DOScale(Vector3.zero, _scaleDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                _popupPanel.SetActive(false);
            });
    }

    private void OnConfirmClicked()
    {
        _onConfirmCallback?.Invoke();
    }

    private void OnCancelClicked()
    {
        _onCancelCallback?.Invoke();
    }
}

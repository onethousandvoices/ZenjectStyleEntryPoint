using System;
using Data;
using UnityEngine;
using UnityEngine.UI;

namespace BaseTemplate.Views.UI
{
    public class MainCanvasView : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _scenesButton;
        [SerializeField] private Button _charactersButton;
        [SerializeField] private DataPresenterView _presenterPrefab;

        private DataPresenterView _dataPresenter;

        /// <summary>
        /// As you said in the task not every ui elements are located in the scene.
        /// But personally i don't understand it.
        /// For me it would be better if canvas will contain these objects, but just turned off.
        /// But maybe i have misunderstood something.. 
        /// </summary>
        public void Awake()
        {
            ViewHolder.AddView(this);
            _dataPresenter = Instantiate(_presenterPrefab, transform);
            _dataPresenter.Init(ShowOnlyStartButton);
        }

        public void Init(Action onStart, Action onScenes, Action onCharacters)
        {
            _startButton.onClick.RemoveAllListeners();
            _scenesButton.onClick.RemoveAllListeners();
            _charactersButton.onClick.RemoveAllListeners();

            _startButton.onClick.AddListener(() => onStart?.Invoke());
            _scenesButton.onClick.AddListener(() => onScenes?.Invoke());
            _charactersButton.onClick.AddListener(() => onCharacters?.Invoke());

            ShowOnlyStartButton();
        }

        public void StartShowcase(BaseData[] datas)
            => _dataPresenter.SetData(datas);

        public void ShowItem(int index)
            => _dataPresenter.ShowItem(index);

        private void ShowOnlyStartButton()
        {
            _startButton.gameObject.SetActive(true);
            _scenesButton.gameObject.SetActive(false);
            _charactersButton.gameObject.SetActive(false);
            _dataPresenter.ShowCloseButton(false);
            _dataPresenter.ClearData();
        }

        public void ShowSideButtons()
        {
            _startButton.gameObject.SetActive(false);
            _scenesButton.gameObject.SetActive(true);
            _charactersButton.gameObject.SetActive(true);
            _dataPresenter.ShowCloseButton(true);
        }

        public void HideAll()
        {
            _startButton.gameObject.SetActive(false);
            _scenesButton.gameObject.SetActive(false);
            _charactersButton.gameObject.SetActive(false);
            _dataPresenter.ShowCloseButton(false);
        }
    }
}
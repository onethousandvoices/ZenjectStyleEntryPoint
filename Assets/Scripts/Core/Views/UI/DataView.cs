using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaseTemplate.Views.UI
{
    public class DataView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _id;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private Image _image;
        [SerializeField] private Button _callbackButton;

        private GameObject _model;

        public void Show(bool state)
        {
            gameObject.SetActive(state);
            if (_model != null)
                _model.SetActive(state);
        }
        
        public void Init(BaseData data)
        {
            _id.text = $"id {data.ID}";
            gameObject.SetActive(true);
            
            //Pool system may also be realized here
            if (_model != null)
                Destroy(_model);
            
            switch (data)
            {
                case SceneData sceneData:
                    _name.text = sceneData.name;
                    _description.text = sceneData.Description;
                    _image.sprite = sceneData.Sprite;
                    _callbackButton.onClick.AddListener(sceneData.Callback);
                    
                    _name.gameObject.SetActive(true);
                    _description.gameObject.SetActive(true);
                    _image.gameObject.SetActive(true);
                    _callbackButton.gameObject.SetActive(true);
                    break;
                case CharacterData characterData:
                    _name.text = characterData.name;
                    _description.text = $"Level {characterData.Level}";
                    _image.sprite = characterData.Avatar;
                    _model = Instantiate(characterData.Model);
                    
                    _name.gameObject.SetActive(true);
                    _description.gameObject.SetActive(true);
                    _image.gameObject.SetActive(true);
                    _callbackButton.gameObject.SetActive(false);
                    break;
            }
        }
    }
}
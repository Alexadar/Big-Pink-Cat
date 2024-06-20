using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.Video;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using System.Linq;
using Unity.VisualScripting;

public class QuestGameComponent : MonoBehaviour {

    private bool isMouseDragging;
    void Update()
    {
        if (Input.GetMouseButton(0) && !isMouseDragging)
        {
            isMouseDragging = true;
            if (current_state == UIState.DIALOG)
            {
                updateDialogText();
            }
        }

        if (!Input.GetMouseButton(0))
        {
            isMouseDragging = false;
        }
    }

    #region Public Variables
        public string game_folder;
        public GameObject game_title;

        public Canvas canvas_start_screen;

        public Canvas canvas_quest_dialog;

        public Canvas canvas_error_message;

        public GameObject quest_dialog_current;

        public RectTransform quest_option_selectors;

        public Button quest_option_button_prefab;   

    #endregion

    #region Private Variables

        private bool metadata_loaded = false;

        private QuestGameModel game_model;


        private VideoPlayer video_player_main;

        private AudioSource audio_source_main;

        private List<VideoClip> loading_clips = new List<VideoClip>();

        private Dialog current_dialog;
    
    #endregion

    #region State Machine

        private enum UIState:int {
            ERROR = -1,
            NONE = 0,
            LOADING = 1,
            GAME_MENU = 2,
            DIALOG = 3,
            DIALOG_OPTIONS = 4,
            FINAL_WORDS = 5
        }

        private UIState current_state = UIState.NONE;

        private bool canChangeState(UIState new_state) {

            if (new_state == UIState.ERROR && current_state != UIState.ERROR) {
                return true;
            }

            switch (current_state) {
                case UIState.NONE:
                    return new_state == UIState.LOADING;
                case UIState.LOADING:
                    return new_state == UIState.GAME_MENU;
                case UIState.GAME_MENU:
                    return new_state == UIState.DIALOG;
                case UIState.DIALOG:
                    return new_state == UIState.DIALOG_OPTIONS || new_state == UIState.FINAL_WORDS;
                case UIState.DIALOG_OPTIONS:
                    return new_state == UIState.DIALOG;
                case UIState.FINAL_WORDS:
                    return new_state == UIState.GAME_MENU;
                case UIState.ERROR:
                    return new_state == UIState.LOADING || new_state == UIState.GAME_MENU;
            }

            return false;
        }

        private void ChangeState(UIState new_state) {
            try {
                if (canChangeState(new_state)) {
                    Debug.Log("Changing state from " + current_state + " --> " + new_state);
                    var old_state = current_state;
                    current_state = new_state;
                    var canvas_start_screen_visible = false;
                    var canvas_quest_dialog_visible = false;
                    var quest_option_selectors_visible = false;
                    var canvas_error_message_visible = false;
                    switch (current_state) {
                        case UIState.NONE:
                            break;
                        case UIState.LOADING:
                            if(!LoadResources()) {
                                throw new Exception("Error loading resources");
                            }
                            break;
                        case UIState.GAME_MENU:
                            canvas_start_screen_visible = true;
                            GameMenu();
                            break;
                        case UIState.DIALOG:
                            canvas_quest_dialog_visible = true;
                            updateDialogText();
                            break;
                        case UIState.DIALOG_OPTIONS:
                            canvas_quest_dialog_visible = true;
                            quest_option_selectors_visible = true;
                            ShowOptions();
                            break;
                        case UIState.FINAL_WORDS:
                            canvas_quest_dialog_visible = true;
                            setFinalWordsOfTheStory();
                            break;
                        case UIState.ERROR:                
                            canvas_error_message_visible = true;
                            // set error mesage in canvas TextMechPro with tag "error_text"
                            break;
                    }
                    set_panel_visibility(canvas_start_screen, canvas_start_screen_visible);
                    set_panel_visibility(canvas_quest_dialog, canvas_quest_dialog_visible);
                    quest_option_selectors.gameObject.SetActive(quest_option_selectors_visible);
                    set_panel_visibility(canvas_error_message, canvas_error_message_visible);
                } else {
                    Debug.LogError("Cannot change state from " + current_state + " --> " + new_state);
                }
            } catch (Exception e) {
                Debug.LogError("Error changing state: ");
                Debug.LogException(e);
                ChangeState(UIState.ERROR);
            }
        }

    #endregion

    # region UIState.LOADING
        bool LoadResources() {
            if (game_folder != null) {
                Debug.Log("Loading game metadata for " + game_folder);

                game_model = new QuestGameModel();
                game_model.loadData(game_folder);
                game_model.loadVideos(game_folder);
                game_model.loadAudios(game_folder);

                foreach(var character in game_model.characters_root.characters) {
                    loading_clips.Add(character.video_clips[0]);
                }
                loading_clips.Add(game_model.game_meta.bg_video_clips[0]);

                video_player_main = GameObject.Find("VideoPlayer_Main").GetComponent<VideoPlayer>();
                audio_source_main = GameObject.Find("AudioSource_Main").GetComponent<AudioSource>();

                video_player_main.clip = loading_clips[Random.Range(0, loading_clips.Count)];
                audio_source_main.clip = game_model.game_meta.bgm_clips[0];

                set_panel_visibility(canvas_start_screen, false);
                set_panel_visibility(canvas_quest_dialog, false);

                metadata_loaded = true;
                Debug.Log("Game metadata loaded");
            } else {
                Debug.LogError("game_folder not set");
                metadata_loaded = false;
            }
            return metadata_loaded;
        }
    # endregion

    # region UIState.GAME_MENU

        public void GameMenu()
        {   
            if (current_state == UIState.GAME_MENU) {
                video_player_main.Play();
                audio_source_main.Play();
                var rolling_text_fade = game_title.GetComponent<RollingTextFade>();
                rolling_text_fade.SetText(game_model.game_meta.game_name);
                rolling_text_fade.SetFade();
                current_dialog = game_model.story_summary.summary_part.dialog_root[0];
            } else {
                Debug.LogError("Game metadata not loaded");
            }
        }

        public void StartGameButtonClicked() {
            ChangeState(UIState.DIALOG);
        }

    # endregion

    # region UIState.DIALOG

        public void updateDialogText() {
            if(current_state == UIState.DIALOG) {
                Debug.Log("Showing dialog: " + current_dialog.character_line);
                var tmpro_text = quest_dialog_current.GetComponent<TextMeshProUGUI>();
                var rolling_text_fade = tmpro_text.GetComponent<RollingTextFade>();
                rolling_text_fade.SetText(current_dialog.character_line);
                rolling_text_fade.SetFade();
                video_player_main.clip = current_dialog.character.video_clips[0];
                if (current_dialog.next_dialog != null) {
                    current_dialog = current_dialog.next_dialog;
                } else {
                    if(current_dialog.options != null && current_dialog.options.Count > 0) {
                        ChangeState(UIState.DIALOG_OPTIONS);
                    } else {
                        if (current_dialog.final_words_of_the_story != null) {
                            ChangeState(UIState.FINAL_WORDS);
                        }
                    }
                }
            } else {
                Debug.LogError("Cannot show dialog from state " + current_state);
            }
        }
    # endregion

    # region UIState.DIALOG_OPTIONS
        private void ShowOptions() {
        var padding = 10;
        var panel_height = quest_option_selectors.GetComponent<RectTransform>().rect.height;
        var panel_width = quest_option_selectors.GetComponent<RectTransform>().rect.width;
        var option_count = current_dialog.options.Count;
        var button_height = ( panel_height - padding * 2 * option_count) / option_count;
        var button_width = panel_width - padding * 2;
        float next_button_y = panel_height / 2 - button_height / 2 - padding;

        quest_option_selectors.gameObject.SetActive(true);

        foreach(var option in current_dialog.options) {
                Debug.Log("Adding option button: " + option.option_text);
                Button button = Instantiate(quest_option_button_prefab, quest_option_selectors.transform);
                
                button.GetComponentInChildren<TextMeshProUGUI>().text = option.option_text;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    current_dialog = game_model.summary_parts[option.next_summary_idx].dialog_root[0];
                    RemoveAllButtons();
                    ChangeState(UIState.DIALOG);
                });
                button.transform.SetParent(quest_option_selectors.transform);
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(button_width, button_height);
                button.GetComponent<RectTransform>().anchoredPosition = new Vector2(padding, next_button_y);

                next_button_y -= button_height + padding;
            }
        }

        private void RemoveAllButtons() {
            foreach (Transform child in quest_option_selectors.transform) {
                Destroy(child.gameObject);
            }
        }
    # endregion

    # region UIState.FINAL_WORDS
        public void setFinalWordsOfTheStory() {
            quest_dialog_current.GetComponent<TextMeshProUGUI>().text = current_dialog.final_words_of_the_story;
            Invoke("resetGame", 5);
        }

        public void resetGame() {
            ChangeState(UIState.GAME_MENU);
        }
    # endregion 

    #region UI Utils
        public void set_panel_visibility(Canvas panel, bool show) {
            panel.gameObject.SetActive(show);
            panel.enabled = show;
        }
    #endregion

    #region Unity Events
        void Start()
        {   
            ChangeState(UIState.GAME_MENU);
        }

        void Awake() {
            ChangeState(UIState.LOADING);
        }
    #endregion

}
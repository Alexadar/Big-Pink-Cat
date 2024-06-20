using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.Networking;
using Unity.VisualScripting;
using UnityEngine.UI;

public class StorySummary
{
    public string current_option { get; set; }

    public string current_summary { get; set; }

    public int index { get; set; }

    public List<StorySummary> next_structs { get; set; }

    public string previous_structure { get; set; }

    public string previous_summary { get; set; }

    [YamlIgnore]
    public SummaryPart summary_part { get; set; }
}

public class GameMeta 
{
    public string game_name { get; set; }

    public string plot_summary { get; set; } 

    [YamlIgnore]
    public List<AudioClip> bgm_clips { get; set; }

    [YamlIgnore]
    public List<VideoClip> bg_video_clips { get; set; }

    [YamlIgnore]
    public int duration_seconds { get; set; }

}

public class SummaryPart
{
    public string current_option { get; set; }
    public string current_summary { get; set; }
    public int index { get; set; }
    public List<string> next_options { get; set; }
    public List<string> next_structs { get; set; }
    public List<int> next_structs_idx { get; set; }
    public string previous_structure { get; set; }
    public string previous_summary { get; set; }

    [YamlIgnore]
    public DialogRoot dialog_root { get; set; }
}

public class Dialog
{
    public int character_id { get; set; }
    public string character_line { get; set; }
    public string character_name { get; set; }
    public int id { get; set; }
    public List<DialogOption> options { get; set; }

    public string final_words_of_the_story { get; set; }

    [YamlIgnore]
    public Character character { get; set; }

    [YamlIgnore]
    public Dialog previous_dialog { get; set; }

    [YamlIgnore]
    public Dialog next_dialog { get; set; }

    [YamlIgnore]
    public int duration_seconds { get; set; }
}

public class DialogOption
{
    public string option_text { get; set; }

    [YamlIgnore]
    public int next_summary_idx { get; set; }
}

public class DialogRoot : List<Dialog>
{
   
}

public class Character
{
    public string description { get; set; }
    public int id { get; set; }
    public string name { get; set; }

    [YamlIgnore]
    public List<VideoClip> video_clips { get; set; }
}

public class CharactersRoot
{
    public List<Character> characters { get; set; }

    public Character findCharacter(int id) {
        foreach (var character in characters) {
            if (character.id == id) {
                return character;
            }
        }
        return null;
    }
}

public class QuestGameModel: MonoBehaviour {
    public GameMeta game_meta;

    public StorySummary story_summary;

    public CharactersRoot characters_root;

    private string prefix_path;

    public Dictionary<int, SummaryPart> summary_parts = new Dictionary<int, SummaryPart>();

    public T DeserializeYaml2<T>(string filePath)
    {
        Debug.Log("Deserializing " + filePath);
        var text = File.ReadAllText(filePath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<T>(text);
    }

    public T DeserializeYaml<T>(string assetPath)
    {
        Debug.Log("Deserializing " + assetPath);
        var text = Resources.Load<TextAsset>(assetPath).text;
        return DeserializeYamlText<T>(text);
    }

    public T DeserializeYamlText<T>(string text)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<T>(text);
    }

    public StorySummary findSummaryPart(SummaryPart summary_part, StorySummary current_story_summary) {
        if (current_story_summary.index == summary_part.index) {
            return current_story_summary;
        } else {
            foreach (var next_struct in current_story_summary.next_structs) {
                var found = findSummaryPart(summary_part, next_struct);
                if (found != null) {
                    return found;
                }
            }
        }
        return null;
    }

    public void loadData(string game_folder) {
        prefix_path = "quests/" + game_folder;
        game_meta = DeserializeYaml<GameMeta>(prefix_path + "/game_meta");
        story_summary = DeserializeYaml<StorySummary>(prefix_path + "/story_summary");
        characters_root = DeserializeYaml<CharactersRoot>(prefix_path + "/characters_meta");

        var summary_parts_folder = prefix_path + "/summary_parts";
        var summary_parts_files = Resources.LoadAll<TextAsset>(summary_parts_folder);

        if (summary_parts_files.Length == 0) {
            Debug.LogError("No summary parts found in " + summary_parts_folder);
        } else {
            Debug.Log("Found " + summary_parts_files.Length + " summary parts");
        }

        foreach (var file in summary_parts_files) {
            Debug.Log("Loading summary part " + file.name);
            print(file.text);
            var summary_part = DeserializeYamlText<SummaryPart>(file.text);
            summary_part.dialog_root = DeserializeYaml<DialogRoot>(prefix_path + "/dialogs/dialog_" + summary_part.index + "");
            summary_parts.Add(summary_part.index, summary_part);

            for(int i = 0; i < summary_part.dialog_root.Count; i++) {
                var dialog = summary_part.dialog_root[i];
                dialog.character = characters_root.findCharacter(dialog.character_id);
                dialog.duration_seconds = 2;

                // set previous and next dialog
                if (i > 0) {
                    summary_part.dialog_root[i].previous_dialog = summary_part.dialog_root[i - 1];
                }
                if (i < summary_part.dialog_root.Count - 1) {
                    summary_part.dialog_root[i].next_dialog = summary_part.dialog_root[i + 1];
                }

                // last item
                if (i == summary_part.dialog_root.Count - 1) {
                    // set next_summary_id for each option for last item
                    if (dialog.options != null && dialog.options.Count > 0) {
                        for(int j = 0; j < dialog.options.Count; j++) {
                            dialog.options[j].next_summary_idx = summary_part.next_structs_idx[j];
                        }
                    }
                }
            }
            findSummaryPart(summary_part, story_summary).summary_part = summary_part;
        }
    }

    public void loadVideos(string game_folder){
        // char video clips
        foreach (var character in characters_root.characters) {
            character.video_clips = new List<VideoClip>();
            var video_path = $"{prefix_path}/video/char{character.id}/latest_u_d";
            Debug.Log($"Loading video {video_path}");
            var clip = Resources.Load<VideoClip>(video_path);
            if (clip != null) {
                Debug.Log("Loaded video" + video_path); 
                character.video_clips.Add(clip);
            } else {
                Debug.LogError($"{video_path} not found");
            }
        }
        // bg video clips
        game_meta.bg_video_clips = new List<VideoClip>();
        var bg_video_path = $"{prefix_path}/video/style/latest_u_d";
        var bg_clip = Resources.Load<VideoClip>(bg_video_path);
        game_meta.bg_video_clips.Add(bg_clip);
    }

    public void loadAudios(string game_folder){
        // bgm clips
        game_meta.bgm_clips = new List<AudioClip>();
        var bgm_path = $"{prefix_path}/audio/bgm";
        var clip = Resources.Load<AudioClip>(bgm_path);
        if (clip != null) {
            Debug.Log("Loaded audio" + bgm_path); 
            game_meta.bgm_clips.Add(clip);
        } else {
            Debug.LogError($"{bgm_path} not found");
        }
    }
}
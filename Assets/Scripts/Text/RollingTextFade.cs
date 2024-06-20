using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public enum RollingTextFadeMode { FadeOut, FadeIn };

public enum RollingTextStates { 
    None,
    Idle, 
    Idle_Requested,
    Fading_Requested,
    Fading, 
    TargetAlpha_Requested,
    TargetAlpha,
    ChangeText_Requested,
    ChangeText
}

public class RollingTextFade : MonoBehaviour
{
    public float FadeSpeed = 0.1f;
    public int RolloverCharacterSpread = 10;
    public Color32 ColorTint = Color.white;

    public RollingTextFadeMode fadeMode = RollingTextFadeMode.FadeIn;

    private TMP_Text m_TextComponent;

    private RollingTextStates m_State_requested = RollingTextStates.None;

    private RollingTextStates m_State = RollingTextStates.Idle;

    private Queue<KeyValuePair<RollingTextStates, object>> m_stateData = new Queue<KeyValuePair<RollingTextStates, object>>();

    public RollingTextStates State {
        get { return m_State; }
        private set {
            Debug.Log("Setting m_State from " + m_State + " to " + value);
            m_State = value;
        }
    }

    void Awake()
    {
        m_TextComponent = GetComponentInParent<TMP_Text>();
        Debug.Log("m_TextComponent is null: " + (m_TextComponent == null));
    }

    void Start()
    {
        m_stateData.Enqueue(new KeyValuePair<RollingTextStates, object>(RollingTextStates.Fading_Requested, null));
        StartCoroutine(StateMachine());
    }

    public byte DefaultAlpha 
    { 
        get 
        { 
            return fadeMode == RollingTextFadeMode.FadeIn ? (byte)0 : (byte)255; 
        } 
    }

    public byte TargetAlpha 
    { 
        get 
        { 
            return (byte)(255 - DefaultAlpha); 
        } 
    }

    public void ResetAlpha()
    {
        Debug.Log("Resetting alpha. DefaultAlpha: " + DefaultAlpha + ", TargetAlpha: " + TargetAlpha);
        Debug.Log("m_TextComponent is null: " + (m_TextComponent == null));
        var color = m_TextComponent.color;
        m_TextComponent.color = new Color(color.r, color.g, color.b, DefaultAlpha);
        m_TextComponent.ForceMeshUpdate();
    }

    public void SetTargetAlpha()
    {
        m_stateData.Enqueue(new KeyValuePair<RollingTextStates, object>(RollingTextStates.TargetAlpha_Requested, null));
    }

    public void SetFadeIn()
    {
        fadeMode = RollingTextFadeMode.FadeIn;
        SetIdle();
    }

    public void SetFade()
    {
        m_stateData.Enqueue(new KeyValuePair<RollingTextStates, object>(RollingTextStates.Fading_Requested, null));
    }

    public void SetIdle()
    {
        m_stateData.Enqueue(new KeyValuePair<RollingTextStates, object>(RollingTextStates.Idle_Requested, null));
    }

    public void SetFadeOut()
    {
        fadeMode = RollingTextFadeMode.FadeOut;
        SetIdle();
    }

    public void SetText(string text)
    {
        m_stateData.Enqueue(new KeyValuePair<RollingTextStates, object>(RollingTextStates.ChangeText_Requested, text));
    }

    private IEnumerator StateMachine() {
        while (true) {
            if(m_stateData.Count > 0) {
                var stateData = m_stateData.Dequeue();

                RollingTextStates State_Requested = stateData.Key;

                Debug.Log("Processing State requested: " + State_Requested);

                // retrieve state change request
                State = State_Requested;

                CancelSubRoutines();

                // state transition
                switch (State) {
                    case RollingTextStates.Idle_Requested:
                        ResetAlpha();
                        State = RollingTextStates.Idle;
                        break;
                    case RollingTextStates.Fading_Requested:
                        ResetAlpha();
                        StartCoroutine(AnimateVertexColorsFade());
                        // sub coroutines started here
                        break;
                    case RollingTextStates.TargetAlpha_Requested:
                        m_TextComponent.color = new Color(m_TextComponent.color.r, m_TextComponent.color.g, m_TextComponent.color.b, TargetAlpha);
                        m_TextComponent.ForceMeshUpdate();
                        State = RollingTextStates.TargetAlpha;
                        break;
                    case RollingTextStates.ChangeText_Requested:
                        m_TextComponent.text = stateData.Value as string;
                        Debug.Log("Text changed to: " + m_TextComponent.text);
                        ResetAlpha();
                        m_TextComponent.ForceMeshUpdate();
                        State = RollingTextStates.ChangeText;
                        break;
                    default:
                        break;
                }
                Debug.Log("State changed to: " + State);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private List<Coroutine> latestCoroutines = new List<Coroutine>();

    private void CancelSubRoutines() {
        foreach (var coroutine in latestCoroutines) {
            if (coroutine != null) {
                StopCoroutine(coroutine);
            }
        }
        latestCoroutines.Clear();
    }

    public IEnumerator AnimateVertexColorsFade()
    {
        State = RollingTextStates.Fading;
        TMP_TextInfo textInfo = m_TextComponent.textInfo;
        int currentCharacter = 0;
        int startingCharacterRange = 0;

        latestCoroutines = new List<Coroutine>();

        while (State == RollingTextStates.Fading)
        {
            int characterCount = textInfo.characterCount;
            byte fadeSteps = (byte)Mathf.Max(1, 255 / RolloverCharacterSpread);


            for (int i = startingCharacterRange; i < currentCharacter + 1; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                Color32[] newVertexColors = textInfo.meshInfo[materialIndex].colors32;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                byte alpha = newVertexColors[vertexIndex].a;

                latestCoroutines.Add(StartCoroutine(Fade(alpha, fadeSteps, newVertexColors, vertexIndex)));

                for (int j = 0; j < 4; j++)
                {
                    newVertexColors[vertexIndex + j] = (Color)newVertexColors[vertexIndex + j] * ColorTint;
                }

                if (alpha == TargetAlpha)
                {
                    startingCharacterRange += 1;
                    if (startingCharacterRange == characterCount)
                    {

                        State = RollingTextStates.TargetAlpha;
                        break;
                    }
                }
            }

            m_TextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            if (currentCharacter + 1 < characterCount) currentCharacter += 1;

            yield return new WaitForSeconds(0.25f - FadeSpeed * 0.01f);            
        }

    }

    IEnumerator Fade(byte alpha, byte fadeSteps, Color32[] newVertexColors, int vertexIndex)
    {
        bool condition = fadeMode == RollingTextFadeMode.FadeIn ? alpha < 255 : alpha > 0;
        int step = fadeMode == RollingTextFadeMode.FadeIn ? fadeSteps : -fadeSteps;

        while (condition)
        {
            alpha = (byte)Mathf.Clamp(alpha + step, 0, 255);

            for (int j = 0; j < 4; j++)
            {
                newVertexColors[vertexIndex + j].a = alpha;
            }

            yield return new WaitForSeconds(0.1f);

            condition = fadeMode == RollingTextFadeMode.FadeIn ? alpha < 255 : alpha > 0;
        }
    }

}
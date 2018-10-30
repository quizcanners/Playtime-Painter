using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if PEGI
using PlayerAndEditorGUI;
#endif
using SharedTools_Stuff;

namespace STD_Animations
{

    public class SoundAnimator : MonoBehaviour, IAnimated_STD_PEGI
    {

        public enum State { Nothing, Play }

        public AudioSource source;
        public List<AudioClip> audioClips = new List<AudioClip>();
        public bool playRandom;
        public int playIndex;
        public int delay;
        float volume = 1;
        public State state;

        AudioClip GetClip() {
            if (audioClips.Count == 0)
                return null;

            if (playRandom)
                playIndex = Random.Range(0, audioClips.Count);

            return audioClips[playIndex];
        }

        public bool DecodeFrame(string tag, string data)   {
            switch (tag) {
                case "rand": playRandom = data.ToBool(); break;
                case "ind": playIndex = data.ToInt(); break;
                case "delay": delay = data.ToInt(); break;
                case "vol": if (source) volume = data.ToFloat(); break;
                case "st": state = (State)data.ToInt(); ProcessStateUpdate(); break;
                default: return false;
            }

            return true;
        }

        public StdEncoder EncodeFrame()
        {
            var cody = new StdEncoder();

            cody.Add_Bool("rand", playRandom);
            if (!playRandom)
                cody.Add("ind", playIndex);
            cody.Add("delay", delay);
            if (source)
                cody.Add("vol", volume);

            // Should be last: it's decoding triggers state update
            cody.Add("st", (int)state);
            return cody;
        }

        public void AnimatePortion(float portion) {
            if (source)
                source.volume = Mathf.Lerp(source.volume, volume, portion);
        }

        public void SetFrame()
        {
            if (source)
                source.volume = volume;
        }

        void ProcessStateUpdate()
        {

            switch (state)
            {
                case State.Nothing: return;
                case State.Play: var cl = GetClip(); if (source && cl) { source.clip = cl; source.Play((ulong)delay); } break;
            }

        }

#if PEGI

        [SerializeField] bool showDependencies;
        public bool Frame_PEGI()
        {
            bool changed = false;

            "Random Play".toggle(ref playRandom).nl();
            if (!playRandom)
                "File To Play:".select(90, ref playIndex, audioClips);

            "Action".editEnum(ref state).nl();

            if ("Valume".edit(ref volume).nl() && source)
                    source.volume = volume;

            changed |= "Audio Clips".edit_List_Obj(ref audioClips).nl();

            "Dependencies".foldout(ref showDependencies).nl();

            if (showDependencies || source == null)
            {
                changed |= "Audio Source".edit(80, ref source);
                if (!source && icon.Add.Click())
                {
                    source = gameObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                }
                pegi.nl();
            }

            return changed;
        }

#endif

        void Start()  =>   ProcessStateUpdate();

    }
}
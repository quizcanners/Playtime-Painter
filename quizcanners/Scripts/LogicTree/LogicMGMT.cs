using UnityEngine;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;


namespace STD_Logic
{
    public class LogicMGMT : ComponentCfg   {

        public static LogicMGMT inst;

        private bool _waiting;
        private float _timeToWait = -1;
        public static int currentLogicVersion;
        public static void AddLogicVersion() => currentLogicVersion++;

        private static int _realTimeOnStartUp;

        public static int RealTimeNow()
        {
            if (_realTimeOnStartUp == 0)
                _realTimeOnStartUp = (int)((DateTime.Now.Ticks - 733000 * TimeSpan.TicksPerDay) / TimeSpan.TicksPerSecond);

            return _realTimeOnStartUp + (int)Time.realtimeSinceStartup;
        }
        
        public override StdEncoder Encode() =>this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;

        public virtual void OnEnable()  =>  inst = this;
        
        public void AddTimeListener(float seconds)
        {
            seconds += 0.5f;
            _timeToWait = !_waiting ? seconds : Mathf.Min(_timeToWait, seconds);
            _waiting = true;
        }

        protected virtual void DerivedUpdate() { }

        public void Update()
        {
            if (_waiting)
            {
                _timeToWait -= Time.deltaTime;
                if (_timeToWait < 0)
                {
                    _waiting = false;
                    AddLogicVersion();
                }
            }

            DerivedUpdate();
        }

        public void Awake() => RealTimeNow();

        #region Inspector
        #if PEGI

        protected override void ResetInspector() {
            inspectedTriggerGroup = -1;
            base.ResetInspector();
        }

        protected virtual void InspectionTabs() {
            icon.Condition.toggle("Trigger groups", ref inspectedItems, 1);
            icon.Close.toggle("Close All", ref inspectedItems, -1);
        }


        [SerializeField] protected int inspectedTriggerGroup = -1;
        [SerializeField] protected int tmpIndex = -1;
        [NonSerialized] private TriggerGroup _replaceReceived;
        [NonSerialized] private bool _inspectReplacementOption;
        public override bool Inspect()
        {
            var changed = false;

            InspectionTabs();

            changed |= base.Inspect().nl();

            if (inspectedItems == 1) {

                if (inspectedTriggerGroup == -1) {

                    #region Paste Options

                    if (_replaceReceived != null) {

                        var current = TriggerGroup.all.GetIfExists(_replaceReceived.IndexForPEGI);
                        var hint = (current != null) ? "{0} [ Old: {1} => New: {2} triggers ] ".F(_replaceReceived.NameForPEGI, current.Count, _replaceReceived.Count) : _replaceReceived.NameForPEGI;
                        
                        if (hint.enter(ref _inspectReplacementOption))
                            _replaceReceived.Nested_Inspect();
                        else
                        {
                            if (icon.Done.ClickUnFocus())
                            {
                                TriggerGroup.all[_replaceReceived.IndexForPEGI] = _replaceReceived;
                                _replaceReceived = null;
                            }
                            if (icon.Close.ClickUnFocus())
                                _replaceReceived = null;
                        }
                    }
                    else
                    {

                        var tmp = "";
                        if ("Paste Messaged STD data".edit(140, ref tmp) || StdExtensions.LoadOnDrop(out tmp)) {

                            var group = new TriggerGroup();
                            group.DecodeFromExternal(tmp);

                            var current = TriggerGroup.all.GetIfExists(group.IndexForPEGI);
                           
                            if (current == null)
                                TriggerGroup.all[group.IndexForPEGI] = group;
                            else {
                                _replaceReceived = group;
                                if (!_replaceReceived.NameForPEGI.SameAs(current.NameForPEGI))
                                    _replaceReceived.NameForPEGI += " replaces {0}".F(current.NameForPEGI);
                            }
                        }



                    }
                    pegi.nl();

                    #endregion

                }

                "Trigger Groups".write(PEGI_Styles.ListLabel); 
                pegi.nl();

                changed |= TriggerGroup.all.Inspect<UnNullableCfg<TriggerGroup>, TriggerGroup>(ref inspectedTriggerGroup);

                if (inspectedTriggerGroup == -1) {
                    "At Index: ".edit(60, ref tmpIndex);
                    if (tmpIndex >= 0 && ExtensionsForGenericCountless.TryGet(TriggerGroup.all, tmpIndex) == null && icon.Add.ClickUnFocus("Create New Group"))
                    {
                        TriggerGroup.all[tmpIndex].NameForPEGI = "Group " + tmpIndex.ToString();//.GetIndex();
                        tmpIndex++;
                    }
                    pegi.nl();
                }
            }

            pegi.nl();

            return changed;
        }
#endif
        #endregion
    }
}
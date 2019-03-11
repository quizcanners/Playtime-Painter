using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace STD_Logic {

    public sealed class TriggerGroup : AbstractKeepUnrecognizedStd, IGotName, IGotIndex, IPEGI, IPEGI_ListInspect {

        public static UnNullableStd<TriggerGroup> all = new UnNullableStd<TriggerGroup>();
       
        private UnNullableStd<Trigger> _triggers = new UnNullableStd<Trigger>();

        public readonly UnNullableStd<UnNullableStdLists<Values>> taggedInts = new UnNullableStd<UnNullableStdLists<Values>>();

        private string _name = "Unnamed_Triggers";
        private int _index;

        #region Getters & Setters 

        public int Count => _triggers.GetAllObjsNoOrder().Count;

        public Trigger this[int index]
        {
            get
            {
                if (index >= 0)
                {
                    var ready = _triggers.GetIfExists(index);
                    if (ready != null)
                        return ready;

                    ready = _triggers[index];
                    ready.groupIndex = IndexForPEGI;
                    ready.triggerIndex = index;
#if PEGI
                    _listDirty = true;
#endif
                    return ready;
                }
                else return null;
            }
        }

        public int IndexForPEGI { get { return _index; } set { _index = value; } }

        public string NameForPEGI { get { return _name; } set { _name = value; } }

        private void Add(string name, ValueIndex arg = null)
        {
            var ind = _triggers.AddNew();
            var t = this[ind];
            t.name = name;
            t.groupIndex = IndexForPEGI;
            t.triggerIndex = ind;

            if (arg != null)
            {
                if (arg.IsBoolean)
                    t.Usage = TriggerUsage.Boolean;
                else
                    t.Usage = TriggerUsage.Number;

                arg.Trigger = t;
            }

            _lastUsedTrigger = ind;

#if PEGI
            _listDirty = true;
#endif
        }

        #endregion

        #region Encode_Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("n", _name)
            .Add("ind", _index)
            .Add_IfNotDefault("t", _triggers)
            .Add("br", _browsedGroup)
            .Add_IfTrue("show", _showInInspectorBrowser)
            .Add("last", _lastUsedTrigger);

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "n": _name = data; break;
                case "ind": _index = data.ToInt(); break;
                case "t":
                    data.DecodeInto(out _triggers);
                    foreach (var t in _triggers){
                        t.groupIndex = _index;
                        t.triggerIndex = _triggers.currentEnumerationIndex;
                    }
                    break;
                case "br": _browsedGroup = data.ToInt(); break;
                case "show": _showInInspectorBrowser = data.ToBool(); break;
                case "last": _lastUsedTrigger = data.ToInt(); break;
                default: return false;
            }
            return true;
        }

        public override void Decode(string data)
        {
#if PEGI
            _listDirty = true;
#endif
            base.Decode(data);
        }

        #endregion

        #region Inspector

        private static int _browsedGroup = -1;
        private bool _showInInspectorBrowser = true;
        private int _lastUsedTrigger;

        public Trigger LastUsedTrigger
        {
            get { return _triggers.GetIfExists(_lastUsedTrigger); }
            set { if (value != null) _lastUsedTrigger = value.triggerIndex; }
        }

        public static Trigger TryGetLastUsedTrigger() => Browsed?.LastUsedTrigger;
       
        public static TriggerGroup Browsed
        {
            get { return _browsedGroup >= 0 ? all[_browsedGroup] : null; }
            set { _browsedGroup = value?.IndexForPEGI ?? -1; }
        }

        #if PEGI
        private bool _listDirty;

        private string _lastFilteredString = "";

        private readonly List<Trigger> _filteredList = new List<Trigger>();

        public List<Trigger> GetFilteredList(ref int showMax, bool showBooleans = true, bool showInts = true)
        {
            if (!_showInInspectorBrowser)
            {
                _filteredList.Clear();
                return _filteredList;
            }

            if (!_listDirty && _lastFilteredString.SameAs(Trigger.searchField))
                return _filteredList;
            else  {

                _filteredList.Clear();
                foreach (var t in _triggers)
                    if ((t.IsBoolean ? showBooleans : showInts) && t.SearchWithGroupName(_name)) {
                        showMax--;

                        Trigger.searchMatchesFound++;

                        _filteredList.Add(t);

                        if (showMax < 0)
                            break;
                    }

                _lastFilteredString = Trigger.searchField;
#if PEGI
                _listDirty = false;
#endif
            }
            return _filteredList;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited) {
            var changed = this.inspect_Name();

            if (icon.Enter.ClickUnFocus())
                edited = ind;

            if (icon.Email.Click("Send this Trigger Group to somebody via email."))
                this.EmailData("Trigger Group {0} [index: {1}]".F(_name, _index), "Use this Trigger Group in your Node Books");

            return changed;
        }
        
        public override bool Inspect()  {

            var changed = false;

            if (inspectedItems == -1) {


                changed |= "{0} : ".F(_index).edit(50, ref _name).nl();


                "Share:".write("Paste message full with numbers and lost of ' | ' symbols into the first line or drop file into second" ,50);
                
                string data;
                if (this.SendReceivePegi("Trigger Group {0} [{1}]".F(_name, _index), "Trigger Groups", out data)) {
                    var tmp = new TriggerGroup();
                    tmp.Decode(data);
                    if (tmp._index == _index) {

                        Decode(data);
                        Debug.Log("Decoded Trigger Group {0}".F(_name));
                    }
                    else
                        Debug.LogError("Pasted trigger group had different index, replacing");
                }

           
                pegi.line();

                "New Variable".edit(80, ref Trigger.searchField);
                AddTriggerToGroup_PEGI();
                
                changed |= _triggers.Nested_Inspect(); 
            }

            return changed;

        }

        private bool AddTriggerToGroup_PEGI(ValueIndex arg = null)
        {
      
            if ((Trigger.searchMatchesFound != 0) || (Trigger.searchField.Length <= 3)) return false;
            
     
            var selectedTrig = arg?.Trigger;

            if (selectedTrig != null && Trigger.searchField.IsIncludedIn(selectedTrig.name)) return false;
            
            var changed = false;

            if (icon.Add.ClickUnFocus("CREATE [" + Trigger.searchField + "]").changes(ref changed)) 
                Add(Trigger.searchField, arg);

            return changed; 
        }

        public static bool AddTrigger_PEGI(ValueIndex arg = null) {

            var changed = false;

            var selectedTrig = arg?.Trigger;

            if (Trigger.searchMatchesFound == 0 && selectedTrig != null && !selectedTrig.name.SameAs(Trigger.searchField)) {

                var goodLength = Trigger.searchField.Length > 3;

                pegi.nl();

                if (goodLength && icon.Replace.ClickUnFocus(
                    "Rename {0} if group {1} to {2}".F(selectedTrig.name, selectedTrig.Group.ToPegiString(), Trigger.searchField)
                    ).changes(ref changed)) selectedTrig.Using().name = Trigger.searchField;
                
                var differentGroup = selectedTrig.Group != Browsed && Browsed != null;

                if (goodLength && differentGroup)
                    icon.Warning.write("Trigger {0} is of group {1} not {2}".F(selectedTrig.ToPegiString(), selectedTrig.Group.ToPegiString(), Browsed.ToPegiString()));

                var groupLost = all.GetAllObjsNoOrder();
                if (groupLost.Count > 0) {
                    var selected = Browsed?.IndexForPEGI ?? -1;

                    if (pegi.select(ref selected, all)) 
                        Browsed = all[selected];
                }
                else
                    "No Trigger Groups found".nl();

                if (goodLength)
                    Browsed?.AddTriggerToGroup_PEGI(arg);
            }

            pegi.nl();

            return changed;
        }
#endif

        #endregion
        
        public TriggerGroup() {
            _index = UnNullableStd<TriggerGroup>.indexOfCurrentlyCreatedUnnulable;
        }
    }


}


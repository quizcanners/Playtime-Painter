using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using PlayerAndEditorGUI;
using UnityEngine;

namespace QuizCannersUtilities
{

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0019 // Use pattern matching
    
    public class EncodedJsonInspector : IPEGI
    {
        private string jsonDestination = "";

        protected JsonBase rootJson;

        protected static void TryDecode(ref JsonBase j)
        {
            if (!(j is JsonString str)) return;

            var tmp = str.TryDecodeString();
            if (tmp != null)
                j = tmp;
            else
                str.dataOnly = true;

        }

        protected static bool DecodeOrInspectJson(ref JsonBase j, bool foldedOut, string name = "")
        {

            var str = j.AsJsonString;

            if (str != null)
            {

                if (str.dataOnly)
                {
                    if (!foldedOut)
                    {

                        name.edit(ref str.data);

                        if (name.Length > 7 && icon.Copy.Click("Copy name to clipboard", 15))
                            GUIUtility.systemCopyBuffer = name;

                    }
                }
                else if (foldedOut && "Decode 1 layer".Click())
                    TryDecode(ref j);


                pegi.nl();
            }


            if (!foldedOut)
                return false;

            var changed = j.Inspect().nl();



            return changed;
        }

        [DerivedList(typeof(JsonString), typeof(JsonClass), typeof(JsonProperty), typeof(JsonList))]
        protected class JsonString : JsonBase, IGotDisplayName
        {

            public bool dataOnly;

            public override JsonString AsJsonString => this;

            public override bool HasNestedData => !dataOnly;

            public string data;

            public string Data
            {
                set
                {
                    data = Regex.Replace(value, @"\t|\n|\r", "");

                    foreach (var c in data)
                        if (c != ' ')
                        {
                            dataOnly = (c != '[' && c != '{');
                            break;
                        }

                    if (!dataOnly)
                    {
                        data = Regex.Replace(data, "{", "{" + Environment.NewLine);
                        data = Regex.Replace(data, ",", "," + Environment.NewLine);
                    }
                }
            }

            public override int CountForInspector() => data.Length;

            public string NameForDisplayPEGI() => data.IsNullOrEmpty() ? "Empty" : data.FirstLine();

            public JsonString() { }

            public JsonString(string data) { Data = data; }

            public override bool Inspect()
            {

                var changed = false;

                if (dataOnly)
                    pegi.edit(ref data).changes(ref changed);
                else
                    pegi.editBig(ref data).changes(ref changed);

                if (changed)
                    dataOnly = false;

                return changed;
            }

            enum JsonDecodingStage { DataTypeDecision, ExpectingVariableName, ReadingVariableName, ExpectingTwoDots, ReadingData }

            public override bool DecodeAll(ref JsonBase thisJson)
            {
                if (dataOnly) return false;
                var tmp = TryDecodeString();
                if (tmp != null)
                {
                    thisJson = tmp;
                    return true;
                }

                dataOnly = true;

                return false;
            }

            public JsonBase TryDecodeString()
            {
                if (data.IsNullOrEmpty())
                {
                    //Debug.LogError("Data is null or empty");
                    return null;
                }

                data = Regex.Replace(data, @"\t|\n|\r", "");

                StringBuilder sb = new StringBuilder();
                int textIndex = 0;
                int openBrackets = 0;
                bool insideTextData = false;
                string variableName = "";

                List<JsonProperty> properties = new List<JsonProperty>();

                List<JsonString> vals = new List<JsonString>();

                var stage = JsonDecodingStage.DataTypeDecision;

                bool isaList = false;

                while (textIndex < data.Length)
                {

                    var c = data[textIndex];

                    switch (stage)
                    {
                        case JsonDecodingStage.DataTypeDecision:

                            if (c != ' ')
                            {

                                if (c == '{')
                                    isaList = false;
                                else if (c == '[')
                                    isaList = true;
                                else if (c == '"')
                                {
                                    stage = JsonDecodingStage.ReadingVariableName;
                                    sb.Clear();
                                    break;
                                }
                                else
                                {
                                    Debug.LogError("Is not collection. First symbol: " + c);
                                    return null;
                                }

                                stage = isaList
                                    ? JsonDecodingStage.ReadingData
                                    : JsonDecodingStage.ExpectingVariableName;

                            }

                            break;


                        case JsonDecodingStage.ExpectingVariableName:
                            if (c != ' ')
                            {
                                if (c == '}' || c == ']')
                                {

                                    int left = data.Length - textIndex;

                                    if (left > 5)
                                        Debug.LogError("End of collection detected a bit too early. Left {0} symbols: {1}".F(left, data.Substring(textIndex)));
                                    // End of collection instead of new element
                                    break;
                                }

                                if (c == '"')
                                {
                                    stage = JsonDecodingStage.ReadingVariableName;
                                    sb.Clear();
                                }
                                else
                                {
                                    Debug.LogError("Was expecting variable name: {0} ".F(data.Substring(textIndex)));
                                    return null;
                                }
                            }

                            break;
                        case JsonDecodingStage.ReadingVariableName:

                            if (c != '"')
                                sb.Append(c);
                            else
                            {
                                variableName = sb.ToString();
                                stage = JsonDecodingStage.ExpectingTwoDots;
                            }

                            break;

                        case JsonDecodingStage.ExpectingTwoDots:

                            if (c == ':')
                            {
                                sb.Clear();
                                insideTextData = false;
                                stage = JsonDecodingStage.ReadingData;
                            }
                            else if (c != ' ')
                            {
                                Debug.LogError("Was Expecting two dots " + data.Substring(textIndex));
                                return null;
                            }


                            break;
                        case JsonDecodingStage.ReadingData:

                            bool needsClear = false;

                            if (c == '"')
                                insideTextData = !insideTextData;

                            if (!insideTextData && (c != ' '))
                            {

                                if (c == '{' || c == '[')
                                    openBrackets++;
                                else
                                {

                                    var comma = c == ',';

                                    var closed = !comma && (c == '}' || c == ']');

                                    if (closed)
                                        openBrackets--;

                                    if ((closed && openBrackets < 0) || (comma && openBrackets <= 0))
                                    {

                                        var dta = sb.ToString();

                                        if (isaList)
                                        {
                                            if (dta.Length > 0)
                                                vals.Add(new JsonString(dta));
                                        }
                                        else
                                            properties.Add(new JsonProperty(variableName, dta));

                                        needsClear = true;

                                        stage = isaList ? JsonDecodingStage.ReadingData : JsonDecodingStage.ExpectingVariableName;
                                    }

                                }
                            }

                            if (!needsClear)
                                sb.Append(c);
                            else
                                sb.Clear();


                            break;
                    }



                    textIndex++;
                }


                /* if (stage == JsonDecodingStage.ReadingData)
                 {
                     if (isaList)
                         vals.Add(new JsonString(sb.ToString()));
                     else
                         properties.Add(new JsonProperty(variableName, sb.ToString()));
                 }*/

                if (isaList)
                    return new JsonList(vals);
                return properties.Count > 0 ? new JsonClass(properties) : null;


            }

        }

        protected class JsonProperty : JsonBase, IGotDisplayName
        {

            public string name;

            public JsonBase data;

            public JsonProperty()
            {
                data = new JsonString();
            }

            public JsonProperty(string name, string data)
            {
                this.name = name;
                this.data = new JsonString(data);
            }

            public bool foldedOut;

            public override int CountForInspector() => 1;

            public override bool DecodeAll(ref JsonBase thisJson) => data.DecodeAll(ref data);

            public static JsonProperty inspected;

            public string NameForDisplayPEGI() => name + (data.HasNestedData ? "{}" : data.GetNameForInspector());

            public override bool Inspect()
            {

                inspected = this;

                var changed = false;

                pegi.nl();

                if (data.CountForInspector() > 0)
                {

                    if (data.HasNestedData)
                        (name + " " + data.GetNameForInspector()).foldout(ref foldedOut);

                    DecodeOrInspectJson(ref data, foldedOut, name).changes(ref changed);
                }
                else
                    (name + " " + data.GetNameForInspector()).write();

                pegi.nl();

                inspected = null;

                return changed;
            }


        }

        protected class JsonList : JsonBase, IGotDisplayName
        {

            private List<JsonBase> values;
            readonly Countless<bool> foldedOut = new Countless<bool>();

            private string previewValue = "";
            private bool previewFoldout;

            public override int CountForInspector() => values.Count;

            public string NameForDisplayPEGI() => "[{0}]".F(values.Count);

            public override bool Inspect()
            {

                var changed = false;


                if (values.Count > 0)
                {
                    var cl = values[0] as JsonClass;
                    if (cl != null && cl.properties.Count > 0)
                    {

                        if (!previewFoldout && icon.Config.ClickUnFocus(15))
                            previewFoldout = true;
                        
                        if (previewFoldout)
                        {
                            "Select value to preview:".nl();

                            if (previewValue.Length > 0 && "NO PREVIEW VALUE".Click().nl())
                            {
                                previewValue = "";
                                previewFoldout = false;
                            }
                            
                            foreach (var p in cl.properties)
                            {
                                if (p.name.Equals(previewValue))
                                {
                                    icon.Next.write();
                                    if ("CURRENT: {0}".F(previewValue).ClickUnFocus().nl())
                                        previewFoldout = false;
                                }
                                else if (p.name.Click().nl())
                                {
                                    previewValue = p.name;
                                    previewFoldout = false;
                                }
                            }
                        }
                    }
                }

                pegi.nl();

                pegi.Indent();

                string nameForElemenet = "";

                var jp = JsonProperty.inspected;

                if (jp != null)
                {
                    string name = jp.name;
                    if (name[name.Length - 1] == 's')
                    {
                        nameForElemenet = name.Substring(0, name.Length - 1);
                    }
                }

                for (int i = 0; i < values.Count; i++)
                {

                    var val = values[i];

                    bool fo = foldedOut[i];

                    if (val.HasNestedData)
                    {

                        var cl = val as JsonClass;

                        string preview = "";

                        if (cl != null && previewValue.Length > 0)
                        {
                            var p = cl.TryGetPropertByName(previewValue);

                            if (p != null)
                                preview = p.data.GetNameForInspector(); //GetNameForInspector();
                            else
                                preview = "missing";
                        }

                        ((preview.Length > 0 && !fo) ? "{1} ({0})".F(previewValue, preview) : "[{0} {1}]".F(nameForElemenet, i)).foldout(ref fo);
                        foldedOut[i] = fo;
                    }

                    DecodeOrInspectJson(ref val, fo).nl();
                    values[i] = val;

                }

                pegi.UnIndent();


                return changed;
            }

            public override bool DecodeAll(ref JsonBase thisJson)
            {

                bool changes = false;

                for (int i = 0; i < values.Count; i++)
                {
                    var val = values[i];
                    if (val.DecodeAll(ref val))
                    {
                        values[i] = val;
                        changes = true;
                    }
                }

                return changes;
            }

            public JsonList() { values = new List<JsonBase>(); }

            public JsonList(List<JsonString> values) { this.values = values.ToList<JsonBase>(); }
        }

        protected class JsonClass : JsonBase, IGotDisplayName
        {
            public List<JsonProperty> properties;

            public JsonProperty TryGetPropertByName(string pname)
            {
                foreach (var p in properties)
                {
                    if (p.name.Equals(pname))
                        return p;
                }

                return null;
            }

            public string NameForDisplayPEGI() => JsonProperty.inspected == null ? "  " :
                (JsonProperty.inspected.foldedOut ? "{" : (" {" + CountForInspector() + "} "));

            public override int CountForInspector() => properties.Count;

            public override bool Inspect()
            {

                var changed = false;

                pegi.Indent();

                for (int i = 0; i < properties.Count; i++)
                    properties[i].Nested_Inspect();

                pegi.UnIndent();


                return changed;
            }

            public JsonClass()
            {
                properties = new List<JsonProperty>();
            }

            public override bool DecodeAll(ref JsonBase thisJson)
            {

                bool changes = false;

                for (int i = 0; i < properties.Count; i++)
                {
                    var val = properties[i] as JsonBase;
                    changes |= val.DecodeAll(ref val);
                }

                return changes;
            }

            public JsonClass(List<JsonProperty> properties)
            {
                this.properties = properties;
            }
        }

        protected abstract class JsonBase : IPEGI, IGotCount
        {

            public JsonBase AsBase => this;

            public virtual JsonString AsJsonString => null;

            public abstract int CountForInspector();

            public abstract bool DecodeAll(ref JsonBase thisJson);

            public virtual bool HasNestedData => true;

            public abstract bool Inspect();
            
        }

        public EncodedJsonInspector() { rootJson = new JsonString(); }

        public EncodedJsonInspector(string data) { rootJson = new JsonString(data); }

        public bool triedToDecodeAll;

        public void TryToDecodeAll()
        {

            triedToDecodeAll = true;

            var rootAsString = rootJson.AsJsonString;

            if (rootAsString != null && !rootAsString.data.IsNullOrEmpty())
            {

                rootAsString.dataOnly = false;

                var sb = new StringBuilder();

                int index = 0;

                while (index < rootAsString.data.Length && rootAsString.data[index] != '{' && rootAsString.data[index] != '[')
                {
                    sb.Append(rootAsString.data[index]);
                    index++;
                }

                jsonDestination = sb.ToString();

                rootAsString.data = rootAsString.data.Substring(index);
            }



            do { } while (rootJson.DecodeAll(ref rootJson));
        }

        public bool Inspect()
        {

            pegi.nl();

            if (icon.Delete.Click())
            {
                triedToDecodeAll = false;
                rootJson = new JsonString();
                jsonDestination = "";
            }

            if (!triedToDecodeAll && "Decode All".Click())
                TryToDecodeAll();

            if (jsonDestination.Length > 5)
                jsonDestination.write();

            pegi.nl();

            return DecodeOrInspectJson(ref rootJson, true);
        }
    }

}
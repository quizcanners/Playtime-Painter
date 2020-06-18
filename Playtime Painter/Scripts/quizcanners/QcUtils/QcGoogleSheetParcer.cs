using PlayerAndEditorGUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;


namespace QuizCannersUtilities
{

    [Serializable]
    public class QcGoogleSheetParcer : IPEGI
    {
        [SerializeField] private string url = "https://docs.google.com/spreadsheets/d/e/2PACX-1vRGGd_RWw4hUqr462Uu24-Vqr9XVj-CsP0TI0RWAYgBKgyL9XG6p_jEqZg87JZBSh5jkargxk7Q1vIC/pub?gid=";
        [SerializeField] private int pageIndex = 0;


        private string UrlGooglePath => "{0}{1}&single=true&output=csv".F(url, pageIndex.ToString());

        private UnityWebRequest request;

        private CsvReader reader;

        public string jsonText;

        public void To<T>(List<T> list)
        {
            var tmpList = JsonConvert.DeserializeObject<List<T>>(jsonText);
            list.Clear();
            list.AddRange(tmpList);
        }
        
        public bool Inspect()
        {
            var changed = false;

            "URL".edit(40, ref url).nl();
            "Page".edit(ref pageIndex).nl();

            if (request == null)
            {
                if ("Download".Click().nl())
                {
                    request = UnityWebRequest.Get(UrlGooglePath);

                    request.SendWebRequest();
                }
            }
            else
            {
                if (request.isDone)
                {
                    "Download finished".nl();
                    
                    if ("Read".Click().nl())
                    {
                        reader = new CsvReader(request);

                        jsonText = ToJson(reader);
                    }
                }
                else
                {
                    "Thread state: ".F(Mathf.FloorToInt(request.downloadProgress * 100).ToString()).nl();

                    if ("Cancel Trhread".Click().nl())
                        request.Dispose();

                }
            }

            if (reader != null && "Columns".foldout().nl())
                "Columns".write_List(reader.columns);
            
            if (jsonText.IsNullOrEmpty() == false)
                "json".writeBig(60, contents: jsonText);
            
            return changed;
        }
        
        private Thread Download(string address, string path)
        {
            var thread = new Thread(() =>
            {
                var client = new WebClient();
                client.DownloadFile(address, path);
            });
            thread.Start();
            return thread;
        }

        private string ToJson(CsvReader csv)
        {
            var result = new StringBuilder();
            result.Append("[");
            for (var rowIndex = 0; rowIndex < csv.rows.Count; rowIndex++)
            {
                var row = csv.rows[rowIndex];
                if (rowIndex != 0 && rowIndex != csv.rows.Count)
                    result.Append(",");
                result.Append("{");
                for (var index = 0; index < row.Keys.Count; index++)
                {
                    var rowKey = row.Keys[index];
                    result.Append("\"");
                    result.Append(rowKey);
                    result.Append("\"");
                    result.Append(":");
                    result.Append("\"");
                    result.Append(row[rowKey]);
                    result.Append("\"");
                    if (index < row.Keys.Count - 1)
                        result.Append(",");
                }

                result.Append("}");
            }

            result.Append("]");

            return result.ToString();
        }
        
        internal class Row
        {
            public OrderedDictionary data;

            public int Length
            {
                get { return data.Count; }
            }

            public int index = -1;
            public List<Row> table;

            public IList<string> Keys
            {
                get { return data.Keys.Cast<string>().ToList(); }
            }

            public string this[string i]
            {
                get { return data[i] as string; }
            }

            public string this[int i]
            {
                get { return data[i] as string; }
            }

            public Row(OrderedDictionary fill)
            {
                data = fill;
            }

            public bool Has(string key)
            {
                return data.Contains(key) && data[key] as string != "";
            }

            public string AtOrDefault(string key, string def = "") => Has(key) ? this[key] : def;
            
            public T EnumOrDefault<T>(string key, T def = default(T))
            {
                if (Has(key))
                    return (T) Enum.Parse(typeof(T), this[key]);
                return def;
            }

            public int Count
            {
                get { return data.Count; }
            }

            public string GetLast(string Key)
            {
                if (Has(Key))
                    return this[Key];
                else
                {
                    var currentRow = this;
                    while (currentRow.prev != null)
                    {
                        currentRow = currentRow.prev;
                        if (currentRow.Has(Key))
                            return currentRow[Key];
                    }

                    throw new FormatException(Key + " not found; at[" + index + "]");
                }
            }

            public Row next = null;
            public Row prev = null;
        }

        internal class CsvCell
        {
            private int x = 0, y = 0;
            List<Row> table;

            public CsvCell(int _x, int _y, List<Row> _table)
            {
                x = _x;
                y = _y;
                table = _table;
            }

            public bool IsAttribute() => table[x].Keys[y].StartsWith("Attr", StringComparison.InvariantCultureIgnoreCase);
            
            public CsvCell GetOffset(int _x, int _y) => new CsvCell(x + _x, y + _y, table);
 
            public bool HasValue() => table != null && x < table.Count && y < table[x].Count && table[x][y] != "";
            
            public string GetValue()
            {
                try
                {
                    if (x < table.Count && y < table[x].Count)
                    {
                        return table[x][y];
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }

                return "";
            }
        }

        internal class CsvReader 
        {
            public List<string> columns;
            public List<Row> rows = new List<Row>();

            public CsvReader(UnityWebRequest content)
            {
                var lines = new StringReader(content.downloadHandler.text);
                using (lines)
                {
                    string line;
                    line = lines.ReadLine();
                    rows.Clear();
                    if (line != null)
                    {
                        columns = line.Split(',').Select(v => v.Trim()).ToList();
                        int rowIndex = 0;
                        while ((line = lines.ReadLine()) != null)
                        {
                            var rawCells = new List<string>();
                            try
                            {
                                bool good = true;
                                string accumulatedCell = "";
                                for (int i = 0; i < line.Length; i++)
                                {
                                    if (line[i] == ',' && good)
                                    {
                                        rawCells.Add(accumulatedCell.Trim());
                                        accumulatedCell = "";
                                    }
                                    else if (line.Length > (i + 1) && line[i] == '"' && line[i + 1] == '"')
                                    {
                                        accumulatedCell += '"';
                                        i++;
                                    }
                                    else if (line[i] == '"')
                                    {
                                        good = !good;
                                    }
                                    else
                                        accumulatedCell += line[i];
                                }

                                rawCells.Add(accumulatedCell);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e.Message + e.StackTrace + ", at row " + rowIndex);
                            }

                            OrderedDictionary cells = new OrderedDictionary();
                            try
                            {
                                int indexCounter = 0;
                                foreach (string cell in rawCells)
                                {
                                    if (indexCounter < columns.Count)
                                        cells[columns[indexCounter]] = cell;
                                    indexCounter++;
                                }

                                rows.Add(new Row(cells));
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e.Message + e.StackTrace + ", at row " + rowIndex);
                            }

                            rowIndex++;
                        }

                        for (int i = 0; i < rows.Count; i++)
                        {
                            if (i > 0)
                            {
                                rows[i].prev = rows[i - 1];
                                rows[i - 1].next = rows[i];
                            }

                            rows[i].index = i;
                            rows[i].table = rows;
                        }
                    }
                }
            }
        }

    }
}
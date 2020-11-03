using PlayerAndEditorGUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace QuizCannersUtilities
{
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0044 // Add readonly modifier
    
    [Serializable]
    public class QcGoogleSheetToCfg : IPEGI
    {

        #region Downloading

        [SerializeField] private string editUrl = "https://docs.google.com/spreadsheets/d/XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX/edit#gid=0";
        [SerializeField] private string url = "https://docs.google.com/spreadsheets/d/e/XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX/pub?";

        [SerializeField] public List<SheetPage> pages = new List<SheetPage>();
        [SerializeField] private int _selectedPage = 0;

        public SheetPage SelectedPage
        {
            get
            {
                _selectedPage = Mathf.Min(_selectedPage, pages.Count - 1);
                return pages.TryGet(_selectedPage);
            }
            set
            {
                if (value == null)
                    return;

                var index = pages.IndexOf(value);
                if (index == -1)
                {
                    pages.Add(value);
                    _selectedPage = pages.Count - 1;
                }
                else
                    _selectedPage = index;


            }
        }

        [Serializable]
        public class SheetPage : IGotDisplayName, IPEGI_ListInspect
        {
            public string pageName;
            public int pageIndex;

            public bool InspectInList(IList list, int ind, ref int edited)
            {
                var changed = false;
                "Name".edit(40, ref pageName).changes(ref changed);
                "#gid=".edit(50, ref pageIndex).changes(ref changed);
                return changed;
            }

            public string NameForDisplayPEGI() => pageName;
        }

        [NonSerialized] private UnityWebRequest request;
       
        public void StartDownload()
        {
            if (pages.IsNullOrEmptyCollection())
                Debug.LogError("No page assigned");
            else
                StartDownload(pages[0]);
        }

        public void StartDownload(SheetPage page)
        {
            request = UnityWebRequest.Get("{0}gid={1}&single=true&output=csv".F(url, page.pageIndex.ToString()));
            request.SendWebRequest();
        }

        public bool IsDownloading() => request != null && !request.isDone && !request.isNetworkError;

        public IEnumerator DownloadingCoro()
        {
            while (IsDownloading())
            {
                yield return null;
            }
        }

        #endregion
        
        #region Inspector
        private int _inspectedStuff = -1;

        public bool Inspect()
        {
            var changed = false;

            pegi.nl();

            if (_inspectedStuff == -1)
            {
                if (request == null)
                {
                    "Page:".select_Index(40, ref _selectedPage, pages);

                    if (pages.Count > _selectedPage && "Download".Click().nl())
                        StartDownload(pages[_selectedPage]);
                }
                else
                {
                    if ("Clear Request".Click())
                    {
                        request.Dispose();
                        request = null;
                    }
                    else
                    if (request.isDone)
                    {
                        "Download finished".nl();
                    }
                    else
                    {
                        "Thread state: ".F(Mathf.FloorToInt(request.downloadProgress * 100).ToString()).nl();

                        if ("Cancel Trhread".Click().nl())
                            request.Dispose();

                    }
                }
            }

            pegi.nl();

            if ("Source".enter(ref _inspectedStuff, 0).nl())
            {
                "Sheet URL (to Edit))".edit(ref editUrl).changes(ref changed);

                if (_inspectedStuff < 1 && "Open".Click())
                    Application.OpenURL(editUrl);

                pegi.nl();

                "Published CSV Urls (to download)".edit(ref url).changes(ref changed);

                pegi.FullWindowService.DocumentationClickOpen(() =>
                    "GoogleSheet->File->Publish To Web-> Publish... Copy link for .csv document");

                pegi.nl();

                if (url != null)
                {
                    var ind = url.LastIndexOf("pub?");
                    if ((ind > 10 && ind < url.Length - 4) && "Clear Url Ending".Click().nl(ref changed))
                    {
                        url = url.Substring(startIndex: 0, length: ind + 4);
                    }
                }

                "Pages".edit_List(ref pages).nl(ref changed);
            }

            return changed;
        }
        #endregion

        #region Reading
        
        public List<string> columns;
        private List<Row> rows = new List<Row>();
        
        public void ToList<T>(ref List<T> list, bool ignoreErrors = true) where T : ICfgDecode, new()
        {
            TryRead();

            list.Clear();

            if (ignoreErrors)
            {
                foreach (var row in rows)
                {
                    var el = new T();

                    for (int i = 0; i < columns.Count; i++)
                    {
                        try
                        {
                            el.Decode(columns[i], row.data[i]);
                            list.Add(el);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                        }
                    }
                }
            }
            else
            {
                foreach (var row in rows)
                {
                    var el = new T();

                    for (int i = 0; i < columns.Count; i++)
                    {
                        el.Decode(columns[i], row.data[i]);
                        list.Add(el);
                    }
                }
            }
        }

        public void To<K, V>(Dictionary<K, V> dic, Func<V, K> keyFactory, bool ignoreErrors = true) where V : ICfgDecode, new()
        {
            List<V> tmpList = new List<V>();
            ToList(ref tmpList, ignoreErrors: ignoreErrors);
            dic.Clear();

            foreach (var l in tmpList)
            {
                var key = keyFactory(l);
                dic[key] = l;
            }
        }

        public void DecodeList_Indexed<T>(List<T> list, bool ignoreErrors = true) where T : ICfgDecode, IGotIndex, new()
        {
            List<T> tmpList = new List<T>();
            ToList(ref tmpList, ignoreErrors: ignoreErrors);

            foreach (var el in tmpList)
            {
                if (el.IndexForPEGI != -1)
                    list.AddOrReplaceByIGotIndex(el);
            }
        }
        
        private void TryRead()
        {
            if (request != null && request.isDone)
            {
                Process(request);
                request = null;
            }
        }

        private void Process(UnityWebRequest content)
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

                        List<CfgData> cellsInRow = new List<CfgData>();
                        try
                        {
                            int columnIndex = 0;
                            foreach (string cell in rawCells)
                            {
                                if (columnIndex >= columns.Count)
                                    break;

                                cellsInRow.Add(new CfgData(cell));
                                columnIndex++;
                            }

                            rows.Add(new Row(cellsInRow));
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message + e.StackTrace + ", at row " + rowIndex);
                        }

                        rowIndex++;
                    }
                }
            }
        }

        internal class Row
        {
            public List<CfgData> data;

            public Row(List<CfgData> list)
            {
                data = list;
            }

        }

        #endregion
    }
}

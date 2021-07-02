using QuizCanners.Inspect;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class QcLog
    {

        public static string IsNull<T>(T _, string context) =>
            "{1}: {0} is not found".F(typeof(T).ToPegiStringType(), context);
        
        public static string CaseNotImplemented(object unimplementedValue) 
            => "Case [{0}] for [{1}] is not implemented".F(
                unimplementedValue.ToString().SimplifyTypeName(),
                unimplementedValue.GetType().ToPegiStringType());

        public static string CaseNotImplemented(object unimplementedValue, string context)
           => "Case [{0}] for [{1}] is not implemented for {2}".F(
               unimplementedValue.ToString().SimplifyTypeName(), 
               unimplementedValue.GetType().ToPegiStringType(),
               context
               );


        public static InspectableLogging LogHandler = new InspectableLogging();

        public class InspectableLogging : IPEGI
        {
            private bool _subscribedToLogs;
            private bool _subscribedToQuit;

            public bool SavingLogs
            {
                get => _subscribedToLogs;
                set
                {
                    if (_subscribedToLogs == value)
                        return;

                    _subscribedToLogs = value;

                    if (_subscribedToLogs)
                    {
                        Application.logMessageReceived += HandleLog;
                        Debug.Log("Subscribed to logs");
                    }
                    else
                    {
                        Debug.Log("Unsubscribing from Logs");
                        Application.logMessageReceived -= HandleLog;
                    }

                    if (!_subscribedToQuit) 
                    {
                        _subscribedToQuit = true;
                        Application.quitting += () => SavingLogs = false;
                    }
                }
            }

            private void HandleLog(string logString, string stackTrace, LogType type)
            {
                if (logs.Count > 300)
                    logs.RemoveRange(0, 150);

                logs.Add(new LogData { Log = logString, Stack = stackTrace, type = type });
            }

            #region Inspector

            private readonly List<LogData> logs = new List<LogData>();
            private readonly CollectionMetaData _logMeta = new CollectionMetaData(labelName: "Logs", showAddButton: false, showEditListButton: false, showCopyPasteOptions: true); // _inspectedLog = -1;
            public void Inspect()
            {
                var sub = SavingLogs;
                if ("Save Logs".toggleIcon(ref sub).nl())
                    SavingLogs = sub;

                if (!_logMeta.IsInspectingElement)
                {
                    if (logs.Count > 10)
                    {
                        if ("Clear All But 5".ClickConfirm(confirmationTag: "Del Logs").nl())
                            logs.RemoveRange(0, logs.Count - 5);
                    }
                    else
                    {
                        if (SavingLogs && "Create Test Logs".Click().nl())
                        {
                            Debug.Log("Debug Log");
                            Debug.LogWarning("Log Warning");
                            Debug.LogError("Log Error");
                            try
                            {
                                int x = 0;
                                int y = 10 / x;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }
                    }
                }

                if (logs.Count == 0)
                    "NO LOGS YET".nl();
                else
                    _logMeta.edit_List(logs);
            }

            #endregion
        }


        private class LogData : IGotReadOnlyName, IPEGI, INeedAttention, ISearchable
        {
            public string Log;
            public string Stack;
            public LogType type;

            public void Inspect()
            {
                "Log:".write_ForCopy(50, Log, showCopyButton: true);
                pegi.nl();
                "Stack: ".write_ForCopy_Big(Stack, showCopyButton: true);
            }

            public string GetNameForInspector() => Log;

            public string NeedAttention()
            {
                switch (type)
                {
                    case LogType.Log:
                    case LogType.Warning:
                        return null;
                    default:
                        return type.ToString().SimplifyTypeName();
                }
            }

            public bool IsContainsSearchWord(string searchWord)
            {
                if (Log.Contains(searchWord))
                    return true;

                return false;
            }
        }

    }
}

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.DebugUI.View.UIToolkit
{
    internal class DebugUILog
    {
        TextElement logTxt;

        StringBuilder messageBuilder = new StringBuilder();
        StringBuilder logBuilder = new StringBuilder();

        const string kWarningColor = "#ffd500";
        const string kErrorColor = "#db4d00";
        const string kInfoColor = "#DEEDF0";

        const int kLogMessagesLimit = 20;
        const float kRemoveMessageAfterSeconds = 10;

        Queue<KeyValuePair<int, float>> logQueue = new Queue<KeyValuePair<int, float>>();

        internal DebugUILog(VisualElement root)
        {
            logTxt = root.Q<TextElement>("log");
        }

        internal void CheckForOutOfDateMessages()
        {

            float time = Time.unscaledTime;
            int removeLength = 0;

            while (logQueue.Count > 0 && time - logQueue.Peek().Value >= kRemoveMessageAfterSeconds)
            {
                removeLength += logQueue.Dequeue().Key;
            }

            if (removeLength > 0)
                RemoveLastMessages(removeLength);
        }

        void RemoveLastMessages(int length)
        {
            logBuilder = logBuilder.Remove(0, length);
            logTxt.text = logBuilder.ToString();
        }

        internal void UpdateLog(string newMessage, Verbosity verbosity)
        {

            if (logQueue.Count == kLogMessagesLimit)
            {
                var toRemove = logQueue.Dequeue();

                logBuilder = logBuilder.Remove(0, toRemove.Key);
            }

            //build new message
            {
                messageBuilder.Clear();

                messageBuilder.Append("<color=");
                switch (verbosity)
                {
                    case Verbosity.Info:
                        messageBuilder.Append(kInfoColor);
                        break;
                    case Verbosity.Warning:
                        messageBuilder.Append(kWarningColor);
                        break;
                    case Verbosity.Error:
                        messageBuilder.Append(kErrorColor);
                        break;
                }
                messageBuilder.Append(">");

                messageBuilder.Append(newMessage);
                messageBuilder.Append("\n");

                messageBuilder.Append("</color>");
            }

            logBuilder.Append(messageBuilder.ToString());
            logQueue.Enqueue(new KeyValuePair<int, float>(messageBuilder.Length, Time.realtimeSinceStartup));
            logTxt.text = logBuilder.ToString();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.ModAPI;

using VRage.Game.ModAPI.Ingame.Utilities;

namespace SC_BlockRestrictions
{
  public class Logger
  {
    List<string> _lines;
    StringBuilder _builder, _temp;
    TextWriter _writer;
    bool _isClosed;
    int _indentLevel;

    public int IndentLevel
    {
      get { return _indentLevel; }
      set { _indentLevel = Math.Max(0, value); }
    }

    public Logger(string filename, bool isDedicated)
    {
      _writer = isDedicated ? MyAPIGateway.Utilities.WriteFileInLocalStorage(filename, typeof(Logger)) : MyAPIGateway.Utilities.WriteFileInWorldStorage(filename, typeof(Logger));
      _builder = new StringBuilder(1024);
      _temp = new StringBuilder(1024);
      _lines = new List<string>(10);
      _isClosed = false;

      Log("Log Started");
    }

    string DateTimeNow => DateTime.Now.ToString("[HH:mm:ss.fff]");

    public void IncreaseIndent() => _indentLevel++;

    public void DecreaseIndent() => _indentLevel = Math.Max(0, _indentLevel - 1);

    public void AddLine(string text)
    {
      if (_indentLevel > 0)
      {
        for (int i = 0; i < _indentLevel; i++)
          _builder.Append("->");

        _builder.Append(' ');
      }

      _builder
        .Append(text)
        .Append('\n');
    }

    public void LogAll(MessageType msgType = MessageType.DEBUG)
    {
      if (_isClosed || _builder.Length == 0)
      {
        _indentLevel = 0;
        return;
      }

      LogAll(string.Empty, msgType);
    }

    public void LogAll(string text, MessageType msgType = MessageType.DEBUG)
    {
      if (_isClosed)
        return;

      if (string.IsNullOrEmpty(text))
      {
        if (_builder.Length == 0)
          return;

        text = _builder.ToString();
        _builder.Clear();
      }
      else
        text = IndentifyText(text);

      _writer?.Write($"{DateTimeNow} [T{System.Environment.CurrentManagedThreadId}] [DS={MyAPIGateway.Utilities.IsDedicated}] {msgType} | {text}");

      if (_builder.Length > 0)
      {
        _writer?.Write(_builder.ToString());
        _builder.Clear();
      }

      _indentLevel = 0;
      _writer?.Flush();
    }

    public void Log(string text, MessageType msgType = MessageType.DEBUG)
    {
      if (_isClosed || string.IsNullOrEmpty(text))
        return;

      _writer?.Write($"{DateTimeNow} [T{System.Environment.CurrentManagedThreadId}] [DS={MyAPIGateway.Utilities.IsDedicated}] {msgType} | {text}\n");
      _writer?.Flush();
    }

    public void Log(StringBuilder text, MessageType msgType = MessageType.DEBUG)
    {
      if (!_isClosed)
        Log(text.ToString(), msgType);
    }

    string IndentifyText(string text)
    {
      if (_indentLevel == 0)
        return text;

      _lines.Clear();
      _temp.Clear();

      StringSegment seg = new StringSegment(text);
      seg.GetLines(_lines);

      for (int i = 0; i < _lines.Count; i++)
      {
        for (int j = 0; j < _indentLevel; j++)
          _temp.Append("->");

        _temp
          .Append(' ').
          Append(_lines[i]).
          Append('\n');
      }

      return _temp.ToString();
    }

    public void ClearCached()
    {
      _builder?.Clear();
      _indentLevel = 0;
    }

    public void Close()
    {
      if (!_isClosed)
      {
        _lines?.Clear();
        _temp?.Clear();
        _builder?.Clear();
        _lines = null;
        _temp = null;
        _builder = null;

        _writer?.Flush();
        _writer?.Close();
        _isClosed = true;
      }
    }

    public static void WriteData(StringBuilder data, string filename)
    {
      using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(filename, typeof(Logger)))
      {
        writer.Write(data);
        writer.Flush();
      }
    }
  }

  public enum MessageType { ERROR, WARNING, DEBUG }
}

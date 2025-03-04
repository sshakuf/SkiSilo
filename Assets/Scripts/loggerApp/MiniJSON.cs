// /*
//  * MiniJSON: A simple JSON parser and serializer for Unity
//  * Based on the work by Matt Schoen: https://gist.github.com/darktable/1411710
//  * 
//  * This version has been slightly modified to work with recent Unity versions.
//  */

// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Globalization;
// using System.Text;

// namespace MiniJSON
// {
//     public static class Json
//     {
//         /// <summary>
//         /// Parses a JSON string into an object
//         /// </summary>
//         public static object Deserialize(string json)
//         {
//             if (string.IsNullOrEmpty(json))
//             {
//                 return null;
//             }

//             return Parser.Parse(json);
//         }

//         /// <summary>
//         /// Converts an object to a JSON string
//         /// </summary>
//         public static string Serialize(object obj)
//         {
//             return Serializer.Serialize(obj);
//         }

//         private sealed class Parser : IDisposable
//         {
//             private const string WORD_BREAK = "{}[],:\"";

//             private StringReader _json;

//             private Parser(string jsonString)
//             {
//                 _json = new StringReader(jsonString);
//             }

//             public static object Parse(string jsonString)
//             {
//                 using (var instance = new Parser(jsonString))
//                 {
//                     return instance.ParseValue();
//                 }
//             }

//             public void Dispose()
//             {
//                 _json.Dispose();
//                 _json = null;
//             }

//             private Dictionary<string, object> ParseObject()
//             {
//                 Dictionary<string, object> table = new Dictionary<string, object>();

//                 // dummy read open curly to confirm object
//                 _json.Read();

//                 while (true)
//                 {
//                     switch (NextToken)
//                     {
//                         case TOKEN.NONE:
//                             return null;
//                         case TOKEN.COMMA:
//                             continue;
//                         case TOKEN.CURLY_CLOSE:
//                             return table;
//                         default:
//                             // name
//                             string name = ParseString();
//                             if (name == null)
//                             {
//                                 return null;
//                             }

//                             // :
//                             if (NextToken != TOKEN.COLON)
//                             {
//                                 return null;
//                             }
//                             // ditch the colon
//                             _json.Read();

//                             // value
//                             table[name] = ParseValue();
//                             break;
//                     }
//                 }
//             }

//             private List<object> ParseArray()
//             {
//                 List<object> array = new List<object>();

//                 // dummy read open bracket to confirm array
//                 _json.Read();

//                 bool parsing = true;
//                 while (parsing)
//                 {
//                     TOKEN nextToken = NextToken;
//                     switch (nextToken)
//                     {
//                         case TOKEN.NONE:
//                             return null;
//                         case TOKEN.COMMA:
//                             continue;
//                         case TOKEN.SQUARED_CLOSE:
//                             parsing = false;
//                             break;
//                         default:
//                             object value = ParseByToken(nextToken);
//                             array.Add(value);
//                             break;
//                     }
//                 }

//                 return array;
//             }

//             private object ParseValue()
//             {
//                 TOKEN nextToken = NextToken;
//                 return ParseByToken(nextToken);
//             }

//             private object ParseByToken(TOKEN token)
//             {
//                 switch (token)
//                 {
//                     case TOKEN.STRING:
//                         return ParseString();
//                     case TOKEN.NUMBER:
//                         return ParseNumber();
//                     case TOKEN.CURLY_OPEN:
//                         return ParseObject();
//                     case TOKEN.SQUARED_OPEN:
//                         return ParseArray();
//                     case TOKEN.TRUE:
//                         return true;
//                     case TOKEN.FALSE:
//                         return false;
//                     case TOKEN.NULL:
//                         return null;
//                     default:
//                         return null;
//                 }
//             }

//             private string ParseString()
//             {
//                 StringBuilder s = new StringBuilder();
//                 char c;

//                 // ditch opening quote
//                 _json.Read();

//                 bool parsing = true;
//                 while (parsing)
//                 {
//                     if (_json.Peek() == -1)
//                     {
//                         parsing = false;
//                         break;
//                     }

//                     c = NextChar;
//                     switch (c)
//                     {
//                         case '"':
//                             parsing = false;
//                             break;
//                         case '\\':
//                             if (_json.Peek() == -1)
//                             {
//                                 parsing = false;
//                                 break;
//                             }

//                             c = NextChar;
//                             switch (c)
//                             {
//                                 case '"':
//                                 case '\\':
//                                 case '/':
//                                     s.Append(c);
//                                     break;
//                                 case 'b':
//                                     s.Append('\b');
//                                     break;
//                                 case 'f':
//                                     s.Append('\f');
//                                     break;
//                                 case 'n':
//                                     s.Append('\n');
//                                     break;
//                                 case 'r':
//                                     s.Append('\r');
//                                     break;
//                                 case 't':
//                                     s.Append('\t');
//                                     break;
//                                 case 'u':
//                                     var hex = new char[4];

//                                     for (int i = 0; i < 4; i++)
//                                     {
//                                         hex[i] = NextChar;
//                                     }

//                                     s.Append((char)Convert.ToInt32(new string(hex), 16));
//                                     break;
//                             }
//                             break;
//                         default:
//                             s.Append(c);
//                             break;
//                     }
//                 }

//                 return s.ToString();
//             }

//             private object ParseNumber()
//             {
//                 string number = GetNextWord();

//                 if (number.IndexOf('.') == -1 && number.IndexOf('e') == -1 && number.IndexOf('E') == -1)
//                 {
//                     long parsedInt;
//                     Int64.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedInt);
//                     return parsedInt;
//                 }

//                 double parsedDouble;
//                 Double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedDouble);
//                 return parsedDouble;
//             }

//             private string GetNextWord()
//             {
//                 StringBuilder word = new StringBuilder();

//                 while (!IsWordBreak(PeekChar))
//                 {
//                     word.Append(NextChar);

//                     if (_json.Peek() == -1)
//                     {
//                         break;
//                     }
//                 }

//                 return word.ToString();
//             }

//             private TOKEN NextToken
//             {
//                 get
//                 {
//                     EatWhitespace();

//                     if (_json.Peek() == -1)
//                     {
//                         return TOKEN.NONE;
//                     }

//                     switch (PeekChar)
//                     {
//                         case '{':
//                             return TOKEN.CURLY_OPEN;
//                         case '}':
//                             _json.Read();
//                             return TOKEN.CURLY_CLOSE;
//                         case '[':
//                             return TOKEN.SQUARED_OPEN;
//                         case ']':
//                             _json.Read();
//                             return TOKEN.SQUARED_CLOSE;
//                         case ',':
//                             _json.Read();
//                             return TOKEN.COMMA;
//                         case '"':
//                             return TOKEN.STRING;
//                         case ':':
//                             return TOKEN.COLON;
//                         case '0':
//                         case '1':
//                         case '2':
//                         case '3':
//                         case '4':
//                         case '5':
//                         case '6':
//                         case '7':
//                         case '8':
//                         case '9':
//                         case '-':
//                             return TOKEN.NUMBER;
//                     }

//                     string word = GetNextWord();

//                     switch (word)
//                     {
//                         case "false":
//                             return TOKEN.FALSE;
//                         case "true":
//                             return TOKEN.TRUE;
//                         case "null":
//                             return TOKEN.NULL;
//                     }

//                     return TOKEN.NONE;
//                 }
//             }

//             private void EatWhitespace()
//             {
//                 while (char.IsWhiteSpace(PeekChar))
//                 {
//                     _json.Read();

//                     if (_json.Peek() == -1)
//                     {
//                         break;
//                     }
//                 }
//             }

//             private char PeekChar
//             {
//                 get
//                 {
//                     return Convert.ToChar(_json.Peek());
//                 }
//             }

//             private char NextChar
//             {
//                 get
//                 {
//                     return Convert.ToChar(_json.Read());
//                 }
//             }

//             private bool IsWordBreak(char c)
//             {
//                 return char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
//             }

//             private enum TOKEN
//             {
//                 NONE,
//                 CURLY_OPEN,
//                 CURLY_CLOSE,
//                 SQUARED_OPEN,
//                 SQUARED_CLOSE,
//                 COLON,
//                 COMMA,
//                 STRING,
//                 NUMBER,
//                 TRUE,
//                 FALSE,
//                 NULL
//             };
//         }

//         private sealed class Serializer
//         {
//             private StringBuilder _builder;

//             private Serializer()
//             {
//                 _builder = new StringBuilder();
//             }

//             public static string Serialize(object obj)
//             {
//                 var instance = new Serializer();

//                 instance.SerializeValue(obj);

//                 return instance._builder.ToString();
//             }

//             private void SerializeValue(object value)
//             {
//                 if (value == null)
//                 {
//                     _builder.Append("null");
//                 }
//                 else if (value is string)
//                 {
//                     SerializeString((string)value);
//                 }
//                 else if (value is bool)
//                 {
//                     _builder.Append((bool)value ? "true" : "false");
//                 }
//                 else if (value is IList)
//                 {
//                     SerializeArray((IList)value);
//                 }
//                 else if (value is IDictionary)
//                 {
//                     SerializeObject((IDictionary)value);
//                 }
//                 else if (value is char)
//                 {
//                     SerializeString(new string((char)value, 1));
//                 }
//                 else
//                 {
//                     SerializeOther(value);
//                 }
//             }

//             private void SerializeObject(IDictionary obj)
//             {
//                 bool first = true;

//                 _builder.Append('{');

//                 foreach (object e in obj.Keys)
//                 {
//                     if (!first)
//                     {
//                         _builder.Append(',');
//                     }

//                     SerializeString(e.ToString());
//                     _builder.Append(':');

//                     SerializeValue(obj[e]);

//                     first = false;
//                 }

//                 _builder.Append('}');
//             }

//             private void SerializeArray(IList anArray)
//             {
//                 _builder.Append('[');

//                 bool first = true;

//                 foreach (object obj in anArray)
//                 {
//                     if (!first)
//                     {
//                         _builder.Append(',');
//                     }

//                     SerializeValue(obj);

//                     first = false;
//                 }

//                 _builder.Append(']');
//             }

//             private void SerializeString(string str)
//             {
//                 _builder.Append('\"');

//                 char[] charArray = str.ToCharArray();
//                 foreach (var c in charArray)
//                 {
//                     switch (c)
//                     {
//                         case '"':
//                             _builder.Append("\\\"");
//                             break;
//                         case '\\':
//                             _builder.Append("\\\\");
//                             break;
//                         case '\b':
//                             _builder.Append("\\b");
//                             break;
//                         case '\f':
//                             _builder.Append("\\f");
//                             break;
//                         case '\n':
//                             _builder.Append("\\n");
//                             break;
//                         case '\r':
//                             _builder.Append("\\r");
//                             break;
//                         case '\t':
//                             _builder.Append("\\t");
//                             break;
//                         default:
//                             int codepoint = Convert.ToInt32(c);
//                             if ((codepoint >= 32) && (codepoint <= 126))
//                             {
//                                 _builder.Append(c);
//                             }
//                             else
//                             {
//                                 _builder.Append("\\u");
//                                 _builder.Append(codepoint.ToString("x4"));
//                             }
//                             break;
//                     }
//                 }

//                 _builder.Append('\"');
//             }

//             private void SerializeOther(object value)
//             {
//                 if (value is float
//                     || value is int
//                     || value is uint
//                     || value is long
//                     || value is double
//                     || value is sbyte
//                     || value is byte
//                     || value is short
//                     || value is ushort
//                     || value is ulong
//                     || value is decimal)
//                 {
//                     _builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
//                 }
//                 else
//                 {
//                     SerializeString(value.ToString());
//                 }
//             }
//         }
//     }

//     // Helper class for StringReader
//     internal class StringReader : IDisposable
//     {
//         private string _s;
//         private int _pos;
//         private int _length;

//         public StringReader(string s)
//         {
//             _s = s;
//             _length = s.Length;
//             _pos = 0;
//         }

//         public int Read()
//         {
//             if (_pos == _length)
//                 return -1;

//             return _s[_pos++];
//         }

//         public int Peek()
//         {
//             if (_pos == _length)
//                 return -1;

//             return _s[_pos];
//         }

//         public void Dispose()
//         {
//             _s = null;
//         }
//     }
// }
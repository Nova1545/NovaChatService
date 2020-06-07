using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChatLib.DataStates;
using Newtonsoft.Json;
using System.Drawing;
using ChatLib.Extras;

namespace ChatLib.Json
{
    public static class JsonSerialization
    {
        public static string Serialize(JsonMessage json)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            using(JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.None;
                writer.WriteStartObject();
                writer.WritePropertyName("Name");
                writer.WriteValue(json.Name);
                writer.WritePropertyName("MessageType");
                writer.WriteValue(json.MessageType);

                writer.WritePropertyName("Content");
                writer.WriteValue(json.Content);

                writer.WritePropertyName("InformationType");
                writer.WriteValue(json.InfomationType);

                writer.WritePropertyName("RequestType");
                writer.WriteValue(json.RequestType);

                writer.WritePropertyName("Color");
                writer.WriteStartArray();

                writer.WriteStartObject();

                writer.WritePropertyName("R");
                writer.WriteValue(json.Color.R);

                writer.WritePropertyName("G");
                writer.WriteValue(json.Color.G);

                writer.WritePropertyName("B");
                writer.WriteValue(json.Color.B);

                writer.WriteEndArray();

                writer.WritePropertyName("EndPoint");
                writer.WriteValue(json.EndPoint);
                writer.WritePropertyName("StatusType");
                writer.WriteValue(json.StatusType);
                writer.WriteEndObject();
            }
            return sb.ToString();
        }

        public static JsonMessage Deserialize(string json)
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(json));
            JsonMessage m = null;
            string name = "";
            while (reader.Read())
            {
                if(reader.Value != null)
                {
                    string value = reader.Value.ToString();
                    if (value == "Name")
                    {
                        name = reader.ReadAsString();                     
                    }
                    else if (value == "MessageType")
                    {
                        m = new JsonMessage(name, (MessageType)reader.ReadAsInt32());
                    }
                    else if(value == "Content")
                    {
                        m.SetContent(reader.ReadAsString());
                    }
                    else if(value == "R")
                    {
                        int r = (int)reader.ReadAsInt32();

                        reader.Read();
                        int g = (int)reader.ReadAsInt32();

                        reader.Read();
                        int b = (int)reader.ReadAsInt32();

                        m.SetColor(NColor.FromRGB(r, g, b));
                    }
                    else if(value == "EndPoint")
                    {
                        m.SetEndpoint(reader.ReadAsString());
                    }
                    else if(value == "StatusType")
                    {
                        m.SetStatusType((StatusType)reader.ReadAsInt32());
                    }
                    else if(value == "InformationType")
                    {
                        m.SetInformationType((InfomationType)reader.ReadAsInt32());
                    }
                    else if(value == "RequestType")
                    {
                        m.SetRequestType((RequestType)reader.ReadAsInt32());
                    }
                    else if(value == "Color") { }
                    else
                    {
                        Console.WriteLine("Unknown Attribute");
                    }
                }
            }
            return m;
        }

        public static Message ToMessage(this JsonMessage json)
        {
            Message m = new Message(json.Name, json.MessageType, json.EndPoint);
            m.SetColor(json.Color);
            m.SetContent(json.Content);
            m.SetStatusType(json.StatusType);
            m.SetInformationType(json.InfomationType);
            m.SetRequestType(json.RequestType);
            return m;
        }

        public static JsonMessage ToJsonMessage(this Message message)
        {
            JsonMessage js = new JsonMessage(message.Name, message.MessageType, message.EndPoint);
            js.SetColor(message.Color);
            js.SetContent(message.Content);
            js.SetStatusType(message.StatusType);
            js.SetInformationType(message.InfomationType);
            js.SetRequestType(message.RequestType);
            return js;
        }

        public static string SerializeRooms(this Dictionary<string, Room> rooms)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.None;
                writer.WriteStartObject();

                writer.WritePropertyName("Rooms");
                writer.WriteStartArray();

                foreach (KeyValuePair<string, Room> room in rooms)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("Name");
                    writer.WriteValue(room.Value.Name);

                    writer.WritePropertyName("ID");
                    writer.WriteValue(room.Value.ID.ToString());

                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            return sb.ToString();
        }

        public static string Serialize(this Dictionary<string, ClientInfo> clients)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.None;
                writer.WriteStartObject();

                writer.WritePropertyName("Users");
                writer.WriteStartArray();

                foreach (KeyValuePair<string, ClientInfo> room in clients)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("Name");
                    writer.WriteValue(room.Value.Name);

                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            return sb.ToString();
        }

        public static Dictionary<string, Room> DeserializeRooms(this string json)
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(json));
            Dictionary<string, Room> Rooms = new Dictionary<string, Room>();

            string name = "";
            int id = 0;

            reader.Read();
            reader.Read();
            while (reader.Read())
            {
                if(reader.Value != null)
                {
                    name = reader.ReadAsString();
                    reader.Read();
                    id = (int)reader.ReadAsInt32();

                    Room r = new Room(name, id);
                    Rooms[r.GUID] = r;
                }
            }
            return Rooms;
        }
    }
}

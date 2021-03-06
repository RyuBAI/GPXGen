﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GXTY_CSharp
{
    public class RunJSON
    {
        public class Position
        {
            public double Latitude { get; private set; }
            public double Longitude { get; private set; }
            public float Elevation { get; private set; }
            public DateTime Time { get; private set; }
            public Position(double lat, double lon, float ele = -1.15f)
            {
                Latitude = lat; Longitude = lon; Elevation = ele; Time = DateTime.Now;
            }
            public void SetTime(DateTime t)
            {
                Time = t;
            }
            public string ToGPX()
            {
                string str = string.Empty;
                str += "<wpt lat=\"" + Latitude + "\" lon=\"" + Longitude + "\">\r\n";
                str += "    <ele>" + Elevation + "</ele>\r\n";
                str += "    <time>" + Time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "Z</time>\r\n";
                str += "</wpt>\r\n";
                return str;
            }

            public string ToJson()
            {
                return "{\"latitude\":" + Latitude + ",\"longitude\":" + Longitude + "}";
            }

            public const double EARTH_RADIUS = 6371000;
            public double Distance(Position p2)
            {
                //经纬转弧度
                double lat1 = ConvertDegreesToRadians(Latitude);
                double lon1 = ConvertDegreesToRadians(Longitude);
                double lat2 = ConvertDegreesToRadians(p2.Latitude);
                double lon2 = ConvertDegreesToRadians(p2.Longitude);

                //差值
                var vLon = Math.Abs(lon1 - lon2);
                var vLat = Math.Abs(lat1 - lat2);

                //球体上的切面，它的圆心即是球心的一个周长最大的圆。
                var h = HaverSin(vLat) + Math.Cos(lat1) * Math.Cos(lat2) * HaverSin(vLon);

                var distance = 2 * EARTH_RADIUS * Math.Asin(Math.Sqrt(h));

                return Math.Abs(distance);
            }
            public static double TotalDistance(List<Position> plist)
            {
                if (plist.Count <= 1) return 0.0;
                double dist = 0.0;
                for (int i = 0; i < plist.Count - 1; i++)
                {
                    dist += plist[i].Distance(plist[i + 1]);
                }
                return dist;
            }

            private static double HaverSin(double theta)
            {
                var v = Math.Sin(theta / 2);
                return v * v;
            }
            /// <summary>
            /// 将角度换算为弧度。
            /// </summary>
            /// <param name="degrees">角度</param>
            /// <returns>弧度</returns>
            private static double ConvertDegreesToRadians(double degrees)
            {
                return degrees * Math.PI / 180;
            }
            private static double ConvertRadiansToDegrees(double radian)
            {
                return radian * 180.0 / Math.PI;
            }

        }

        public List<Position> PositionList { get; private set; }

        public RunJSON(Position start)
        {
            PositionList = new List<Position> { start };
        }

        public void AddPosition(Position p)
        {
            PositionList.Add(p);
        }
        public void AutoAddPosition(Position delta, int num,float dt = 1f)
        {
            Position start = PositionList.Last();
            for (int i = 1; i <= num; i++)
            {
                Position p = new Position(start.Latitude + delta.Latitude * (i - 1), start.Longitude + delta.Longitude * (i - 1));
                p.SetTime(start.Time.AddSeconds(i * dt));
                AddPosition(p);
            }
        }

        public double TotalDistance()
        {
            return Position.TotalDistance(PositionList);
        }
        public void DistributeTimeSpan(TimeSpan span,DateTime? start = null)
        {
            if (PositionList.Count == 0) return;
            if (start != null) PositionList[0].SetTime((DateTime)start);
            if (PositionList.Count < 2) return;

            double totaldist = TotalDistance();
            for (int i = 1; i < PositionList.Count; i++)
            {
                double thisdist = PositionList[i].Distance(PositionList[i - 1]);
                double rate = thisdist / totaldist;
                //Console.WriteLine(rate);
                double thisspan = span.TotalMilliseconds * rate;
                PositionList[i].SetTime(PositionList[i - 1].Time + TimeSpan.FromMilliseconds(thisspan));
            }
        }

        public string ToGPX()
        {
            string str = "<?xml version=\"1.0\"?>\r\n<gpx version=\"1.1\">\r\n";
            foreach (Position p in PositionList) str += p.ToGPX();
            str += "</gpx>";

            return str;
        }
        /// <summary>
        /// 生成数据包
        /// </summary>
        /// <param name="runpgid">本次跑步id，点击开始跑时服务器会发送过来这个值</param>
        /// <param name="userid">用户id，不会变</param>
        /// <returns></returns>
        public string ToJSON(string runpgid, string userid,string bNode = "",string tNode="")
        {
            string RunType = (bNode == "" && tNode == "") ? "1" : "2";
            string StartT = PositionList.First().Time.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
            string EndT = PositionList.Last().Time.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");
            double Duration = (PositionList.Last().Time - PositionList.First().Time).TotalSeconds;
            double Distance = Position.TotalDistance(PositionList);
            TimeSpan Speed = TimeSpan.FromSeconds(Duration / (Distance / 1000));

            string str = string.Empty;
            str += "{";
            if(bNode == "")str += "\"bNode\":[],";
            else str += "\"bNode\":" + bNode + ",";
            str += "\"buPin\":\""+ new Random().Next(130,170)+ "\",";
            str += "\"duration\":\"" + (int)Duration + "\",";
            str += "\"endTime\":\"" + EndT + "\",";
            str += "\"frombp\":\"0\",";
            str += "\"goal\":\"2.00\",";
            str += "\"real\":\"" + Distance + "\",";
            str += "\"runPageId\":\"" + runpgid + "\",";
            str += "\"speed\":\"" + Speed.Hours.ToString("00") + "\\u0027" + Speed.Minutes.ToString("00") + "\\u0027\\u0027\",";
            str += "\"startTime\":\"" + StartT + "\",";
            if (tNode == "") str += "\"tNode\":[],";
            else str += "\"tNode\":" + tNode + ",";
            str += "\"totalNum\":\"0\",";

            str += "\"track\":[";
            foreach (Position p in PositionList)
            {
                str += p.ToJson();
                if (p != PositionList.Last()) str += ",";
            }
            str += "],";


            str += "\"trend\":[";
            str += "{" +
                "\"x\":0.1,\"y\":50" +
                "\"x\":0.2,\"y\":49.99995" +
                "\"x\":0.3,\"y\":50.001" +
                "\"x\":0.4,\"y\":49.99995" +
                "\"x\":0.5,\"y\":50" +
                "\"x\":0.6,\"y\":50" +
                "\"x\":0.7,\"y\":49.99995" +
                "\"x\":0.8,\"y\":50" +
                "\"x\":0.9,\"y\":50" +
                "}";
            str += "],";

            str += "\"type\":\""+RunType+"\",";
            str += "\"userid\":\"" + userid + "\"";
            str += "}";

            return str.Replace(" ","").Replace("\n","");
        }

        public void WriteGPX(string path)
        {
            if (File.Exists(path))
                File.Delete(path);

            using (StreamWriter writer = new StreamWriter(path, true))
                writer.WriteLine(ToGPX());
        }
        public bool LoadGPX(string path)
        {
            if (!File.Exists(path)) return false;

            List<Position> pList = new List<Position>();

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlNode root = doc.DocumentElement.SelectSingleNode("/gpx");
                XmlNodeList wptlist = root.SelectNodes("wpt");

                for (int i = 0; i < wptlist.Count; i++)
                {
                    double lat = Convert.ToDouble(wptlist[i].Attributes["lat"].Value);
                    double lon = Convert.ToDouble(wptlist[i].Attributes["lon"].Value);

                    float ele = Convert.ToSingle(wptlist[i].ChildNodes[0].InnerText);
                    DateTime t;
                    if (i == 0) t = DateTime.Now;
                    else t = pList.Last().Time +
                            (Convert.ToDateTime(wptlist[i].ChildNodes[1].InnerText.Replace("T", " ").Replace("Z", "")) -
                            Convert.ToDateTime(wptlist[i - 1].ChildNodes[1].InnerText.Replace("T", " ").Replace("Z", "")));

                    Position p = new Position(lat, lon, ele);
                    p.SetTime(t);
                    pList.Add(p);
                }
            }catch(Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            PositionList = pList;
            return true;
        }
    }
}

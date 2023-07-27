using LiveChartsCore.Geo;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace HMI
{
    internal class SystemInfoManager
    {
        private SqlConnection sqlConnection;
        private Dictionary<string, object> RamInfo = new Dictionary<string, object>();
        private Dictionary<string, object> hddInfo = new Dictionary<string, object>();
        private Dictionary<string, object> netInfo = new Dictionary<string, object>();
        private Dictionary<string, object> dbInfo = new Dictionary<string, object>();
        private Dictionary<string, object> cpuUsageInfo = new Dictionary<string, object>();
        public SystemInfoManager(SqlConnection sqlConn) 
        {
            sqlConnection = sqlConn;
            getRamInfo();
            getHddInfo();
            getCpuUsageInfo();
            getNetInfo();
            getdbInfo();
        }

        public Dictionary<string, object> getInfoRam() { return RamInfo; }
        public Dictionary<string, object> getInfoHdd() { return hddInfo; }
        public Dictionary<string, object> getInfoNet() { return netInfo; }
        public Dictionary<string, object> getInfoDb() { return dbInfo; }
        public Dictionary<string, object> getInfoCpuUsage() { return cpuUsageInfo; }

        private void getdbInfo()
        {
            lock (sqlConnection)
            {
                try
                {
                    sqlConnection.Open();
                    object valueSent;
                    if (dbInfo.TryGetValue("Status", out valueSent) == false)
                    {
                        dbInfo.Add("Status", "Database: Connected");
                    }
                    else
                    {
                        if (valueSent != null)
                        {
                            dbInfo["Status"] = "Database: Connected";
                        }
                    }

                    if (dbInfo.TryGetValue("Tip", out valueSent) == false)
                    {
                        dbInfo.Add("Tip", String.Empty);
                    }
                    else
                    {
                        if (valueSent != null)
                        {
                            dbInfo["Tip"] = String.Empty;
                        }
                    }
                    if (dbInfo.TryGetValue("bgColor", out valueSent) == false)
                    {
                        dbInfo.Add("bgColor", Brushes.Green);
                    }
                    else
                    {
                        if (valueSent != null)
                        {
                            dbInfo["bgColor"] = Brushes.Green;
                        }
                    }
                }
                catch (Exception ex)
                {
                    dbInfo["Status"] = "Database: Not Connected";
                    dbInfo["Tip"] = ex.Message;
                    dbInfo["bgColor"] = Brushes.Red;
                }
                sqlConnection.Close();
            }
        }

        private void getNetInfo()
        {
            string ehtStatus = OperationalStatus.NotPresent.ToString();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            bool netFound = false;
            int netNum, i = 0;
            NetworkInterface netItem;
			long prevTotalBytesReceived = 0;
            long prevTotalBytesSent = 0;

            netNum = interfaces.Count();
            netInfo["Status"] = "ETH Status: Device not present";
            netInfo["Sent"] = "Bytes Sent: 0";
            netInfo["Received"] = "Bytes Received: 0";
            netInfo["Raw Sent"] = 0;
            netInfo["Raw Received"] = 0;
            while (!netFound && (i < netNum))
            {
                netItem = interfaces[i];
                if (netItem.Id == "{025780C5-AF34-434F-9D17-07DC5E3AF043}")
                {
                    ehtStatus = netItem.OperationalStatus.ToString();
                    netFound = true;
					object valueRec, valueSent,val1,val2,val4;
                    if (!netInfo.TryGetValue("Status", out val4))
                    { netInfo.Add("Status", "ETH Status: " + ehtStatus); }
                    else { netInfo["Status"] = "ETH Status: " + ehtStatus; }
                    if (!netInfo.TryGetValue("Sent",out val1))
                    { netInfo.Add("Sent", "Bytes Sent: " + netItem.GetIPv4Statistics().BytesSent.ToString()); }
                    else { netInfo["Sent"] = "Bytes Sent: " + netItem.GetIPv4Statistics().BytesSent.ToString(); }
                    if (!netInfo.TryGetValue("Received", out val2))
                    { netInfo.Add("Received", "Bytes Received: " + netItem.GetIPv4Statistics().BytesReceived.ToString()); }
                    else { netInfo["Received"] = "Bytes Received: " + netItem.GetIPv4Statistics().BytesReceived.ToString(); }

                  
                    if (netInfo.TryGetValue("Raw Sent", out valueSent) == false)
                    {
                        netInfo.Add("Raw Sent", (netItem.GetIPv4Statistics().BytesSent));
                    }
                    else
                    {
                        if (valueSent != null)
                        {
                        
                           prevTotalBytesSent = Convert.ToInt64(netInfo["Raw Sent"]);

                           netInfo["Raw Sent"] = netItem.GetIPv4Statistics().BytesSent;
                        }
                    }
                     if (netInfo.TryGetValue("Raw Received", out valueRec) == false)
                    {
                          netInfo.Add("Raw Received", (netItem.GetIPv4Statistics().BytesReceived));
                    }
                    else
                    {
                        if (valueRec != null)
                        {
                         
                            prevTotalBytesReceived = Convert.ToInt64(netInfo["Raw Sent"]);

                            netInfo["Raw Received "] = netItem.GetIPv4Statistics().BytesReceived;
                        }
                    }
                    if ((netItem.OperationalStatus != OperationalStatus.Up) || (netItem.NetworkInterfaceType == NetworkInterfaceType.Tunnel) || (netItem.NetworkInterfaceType == NetworkInterfaceType.Loopback))
                    {
                        netFound = false;
                    }

                    var statistics = netItem.GetIPv4Statistics();
                    long tR =statistics.BytesReceived;
                    long tS =statistics.BytesSent ;
                    if ( (tR > prevTotalBytesReceived) && ( tS> prevTotalBytesSent))
                    {
                        netFound = true;
                    }
                }
                i++;
            }

            object val3;
            if (netFound)
            {
                if (!netInfo.TryGetValue("bgColor", out val3))
                { netInfo.Add("bgColor", Brushes.Green); }
                else { netInfo["bgColor"] = Brushes.Green; }

            }
            else
            {
                if (!netInfo.TryGetValue("bgColor", out val3))
                { netInfo.Add("bgColor", Brushes.Red); }
                else {netInfo["bgColor"] = Brushes.Red; }
            }
        }

        private void getCpuUsageInfo()
        {
            string ret;
            double cpuPercentageUse, currentsp;
            PerformanceCounter cpuCounter;
            object val1, val2, val3, val4;

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuPercentageUse = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            cpuPercentageUse = Math.Round(cpuCounter.NextValue(), 4);

            cpuCounter = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
            currentsp = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            currentsp = cpuCounter.NextValue();


            foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT *, Name FROM Win32_Processor").Get())
            {
                double maxSpeed = Convert.ToDouble(obj["MaxClockSpeed"]) / 1000;
                currentsp = Math.Round(maxSpeed * currentsp / 100, 2);
            }







            if (!cpuUsageInfo.TryGetValue("CPU", out val1))
            { cpuUsageInfo.Add("CPU", "CPU %: " + cpuPercentageUse.ToString()); }
            else { cpuUsageInfo["CPU"] = "CPU %: " + cpuPercentageUse.ToString(); }

            if (!cpuUsageInfo.TryGetValue("CPU RAW", out val2))
            { cpuUsageInfo.Add("CPU RAW", cpuPercentageUse); }
            else { cpuUsageInfo["CPU RAW"] = cpuPercentageUse; }

            if (!cpuUsageInfo.TryGetValue("CPU Freq", out val2))
            { cpuUsageInfo.Add("CPU Freq", currentsp.ToString() + "GHz"); }
            else { cpuUsageInfo["CPU Freq"] = currentsp.ToString() + "GHz"; }

            if (cpuPercentageUse < 50)
            {
                if (!cpuUsageInfo.TryGetValue("bgColor", out val3))
                { cpuUsageInfo.Add("bgColor", Brushes.Green); }
                else { cpuUsageInfo["bgColor"] = Brushes.Green; }
            }
            else
            {
                if (!cpuUsageInfo.TryGetValue("bgColor", out val3))
                { cpuUsageInfo.Add("bgColor", Brushes.Red); }
                else { cpuUsageInfo["bgColor"] = Brushes.Red; }
            }
        }

        private void getHddInfo()
        {
            long totalHddSizeMB = 0, freeHddSizeMB = 0, usedHddSizeMB = 0, usedHddSizeGB = 0, percentageUsedHDD = 0;
            ManagementObject disk = new ManagementObject("win32_logicaldisk.deviceid=\"c:\"");
            object val1, val2, val3,val4, val5;

            disk.Get();
            totalHddSizeMB = Convert.ToInt64(disk["Size"]) / 1048576;
            freeHddSizeMB = Convert.ToInt64(disk["FreeSpace"]) / 1048576;
            usedHddSizeMB = totalHddSizeMB - freeHddSizeMB;
            usedHddSizeGB = usedHddSizeMB / 1024;
            percentageUsedHDD  = usedHddSizeMB * 100 / totalHddSizeMB;


            if (!hddInfo.TryGetValue("Total", out val2))
            { hddInfo.Add("Total", "Total HDD: " + totalHddSizeMB.ToString() + " MB"); }
            else { hddInfo["Total"] = "Total HDD: " + totalHddSizeMB.ToString() + " MB"; }

            if (!hddInfo.TryGetValue("UsedMB", out val1))
            { hddInfo.Add("UsedMB", "Used HDD: " + usedHddSizeMB.ToString() + " MB"); }
            else { hddInfo["UsedMB"] = "Used HDD: " + usedHddSizeMB.ToString() + " MB"; }

            if (!hddInfo.TryGetValue("Free", out val3))
            { hddInfo.Add("Free", "Free HDD: " + freeHddSizeMB.ToString() + " MB"); }
            else { hddInfo["Free"] = "Free HDD: " + freeHddSizeMB.ToString() + " MB"; }

            if (!hddInfo.TryGetValue("UsedGB", out val1))
            { hddInfo.Add("UsedGB", usedHddSizeGB.ToString() + " GB"); }
            else { hddInfo["UsedGB"] = usedHddSizeGB.ToString() + " GB"; }

            if (!hddInfo.TryGetValue("PercentageHDD", out val4))
            { hddInfo.Add("PercentageHDD", percentageUsedHDD); }
            else { hddInfo["PercentageHDD"] = percentageUsedHDD; }

            if (percentageUsedHDD < 50)
            {
                if (!hddInfo.TryGetValue("bgColor", out val3))
                { hddInfo.Add("bgColor", Brushes.Green); }
                else { hddInfo["bgColor"] = Brushes.Green; }
            }
            else
            {
                if (!hddInfo.TryGetValue("bgColor", out val3))
                { hddInfo.Add("bgColor", Brushes.Red); }
                else { hddInfo["bgColor"] = Brushes.Red; }
            }

        }

        private void getRamInfo()
        {
            long totalRamSizeMB = 0, freeRamSizeMB = 0, usedRamSizeMB = 0, usedRamSizeGB = 0, percentageUsedRam=0;
            ManagementObjectSearcher SearchRam = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            object val1, val2, val3, val4, val5;

            foreach (ManagementObject Mobject in SearchRam.Get())
            {
                totalRamSizeMB = Convert.ToInt64(Mobject["TotalVisibleMemorySize"]) / 1024;
                freeRamSizeMB = Convert.ToInt64(Mobject["FreePhysicalMemory"]) / 1024;
            }
            
            usedRamSizeMB = totalRamSizeMB - freeRamSizeMB;
            usedRamSizeGB = usedRamSizeMB / 1024;
            percentageUsedRam = usedRamSizeMB * 100 / totalRamSizeMB;
            

            if (!RamInfo.TryGetValue("Total", out val2))
            { RamInfo.Add("Total", "Total Ram: " + totalRamSizeMB.ToString() + " MB"); }
            else { RamInfo["Total"] = "Total Ram: " + totalRamSizeMB.ToString() + " MB"; }

            if (!RamInfo.TryGetValue("UsedMB", out val1))
            { RamInfo.Add("UsedMB", "Used Ram: " + usedRamSizeMB.ToString() + " MB"); }
            else { RamInfo["UsedMB"] = "Used Ram: " + usedRamSizeMB.ToString() + " MB"; }

            if (!RamInfo.TryGetValue("Free", out val3))
            { RamInfo.Add("Free", "Free Ram: " + freeRamSizeMB.ToString() + " MB"); }
            else { RamInfo["Free"] = "Free Ram: " + freeRamSizeMB.ToString() + " MB" ; }

            if (!RamInfo.TryGetValue("UsedGB", out val5))
            { RamInfo.Add("UsedGB", usedRamSizeGB.ToString() + " GB"); }
            else { RamInfo["UsedGB"] = usedRamSizeGB.ToString() + " GB"; }

            if (!RamInfo.TryGetValue("PercentageRam", out val4))
            { RamInfo.Add("PercentageRam", percentageUsedRam); }
            else { RamInfo["PercentageRam"] = percentageUsedRam; }

            if(percentageUsedRam < 50)
            {

                if (!RamInfo.TryGetValue("bgColor", out val3))
                { RamInfo.Add("bgColor", Brushes.Green); }
                else { RamInfo["bgColor"] = Brushes.Green; }
            }
            else
            {
                if (!RamInfo.TryGetValue("bgColor", out val3))
                { RamInfo.Add("bgColor", Brushes.Red); }
                else { RamInfo["bgColor"] = Brushes.Red; }
            }
        }
		public void Update()
        {
            getRamInfo();
            getHddInfo();
            getCpuUsageInfo();
            getNetInfo();
            getdbInfo();
        }					
    }
}

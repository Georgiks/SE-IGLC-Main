IGLC_Main stream;

public Program() {
    stream = new IGLC_Main(this, "LCD Centre 5 Base");
}

void Main(string argument) {
    var fTime = DateTime.Now.Millisecond;
    if (argument != "") {
        stream.BroadcastMessage("ALL", argument, false);
        return;
    }
    stream.ProcessTick();
    Echo("INIT>> "+Runtime.CurrentInstructionCount+"/"+Runtime.MaxInstructionCount+"\n"+
        "Runtime:   "+(DateTime.Now.Millisecond-fTime) + " ms");
}

class IGLC_Main {
    string myID = "B0";
    int updateTick = 1;                        // Every which tick update antenna list
    int oldMsgTimeout = 5*60;

    Dictionary<string, string> logPanelTable = new Dictionary<string, string>() {
            {"Laser Antenna Base", "LCD Centre Base"},
            {"Laser Antenna 2 Base", "LCD Centre 2 Base"}
    };
    Dictionary<IMyLaserAntenna, IGLC> antennaList = new Dictionary<IMyLaserAntenna, IGLC>();
    Dictionary<string, DateTime> sentData = new Dictionary<string, DateTime>();
    List<string> receivedData = new List<string>();
    MyGridProgram program;
    public string log;
    int logLinesCount = 10;
    string lcdPanelName;
    int messageID = 0;
    int runTick = 0;

    public IGLC_Main(MyGridProgram me, string panel = "") {
        program = me;
        lcdPanelName = panel;

        RefreshAntennaList();
    }

    public void ProcessTick() {
        var dictElements = new List<IMyLaserAntenna>(antennaList.Keys);
        for (var i = 0; i < dictElements.Count; i++) {
            antennaList[dictElements[i]].ProcessTick();
        }
        var panel = program.GridTerminalSystem.GetBlockWithName(lcdPanelName) as IMyTextPanel;
        if (panel != null) {
            var toShow = "Received data:\n";
            for (var i = 0; i < receivedData.Count; i++){
                var dat = receivedData[i].Split('|');
                toShow += string.Format("ID: {0}, Text: {1}",dat[1], dat[3])+"\n";
            }
            panel.WritePublicText(toShow+"==================\n"+log);
        }

        RefreshAntennaList();
        if (runTick > 1000) runTick = 0;
        runTick++;
    }

    public void BroadcastMessage(string to, string text, bool noValidy = false,
                                    List<IMyLaserAntenna> except = null) { 
        var toSend = string.Format("{4}|{0}_{1}|{2}|{3}", myID, messageID, to, text, (noValidy ? "+" : "")); 
        messageID++; 
        var dictElements = new List<IMyLaserAntenna>(antennaList.Keys); 
        for (var i = 0; i < dictElements.Count; i++) {
            if (except != null) {
                if (!except.Contains(dictElements[i])) {
                    antennaList[dictElements[i]].SendMessage(toSend);
                }
            } else antennaList[dictElements[i]].SendMessage(toSend);
        } 
        sentData.Add(toSend, DateTime.Now);
    }
    public void BroadcastMessage(string text, List<IMyLaserAntenna> except = null) {  
        var toSend = text;
        var dictElements = new List<IMyLaserAntenna>(antennaList.Keys);  
        for (var i = 0; i < dictElements.Count; i++) {
            if (except != null) { 
                if (!except.Contains(dictElements[i])) { 
                    antennaList[dictElements[i]].SendMessage(toSend); 
                } 
            } else antennaList[dictElements[i]].SendMessage(toSend);
        }  
        sentData.Add(toSend, DateTime.Now);  
    }
    public void BroadcastMessageData(IMyLaserAntenna ant, string data) {
        if (antennaList.ContainsKey(ant)) {
            antennaList[ant].SendMessage(data); 
        }
    }
    void RefreshAntennaList() {
        var antennas = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(antennas);
        for (var i = 0; i < antennas.Count; i++) {
            var antenna = antennas[i] as IMyLaserAntenna;
            if (!antennaList.ContainsKey(antenna)) {
                var logLCDname = "";
                if (logPanelTable.ContainsKey(antenna.CustomName))
                    logLCDname = logPanelTable[antenna.CustomName];
                antennaList[antenna] = new IGLC(program, this, antenna.CustomName, logLCDname);
            }
        }
        var dictKeys = new List<IMyLaserAntenna>(antennaList.Keys);
        for (var i = 0; i < dictKeys.Count; i++) {
            if (!antennas.Contains(dictKeys[i]))
                antennaList.Remove(dictKeys[i]);
        }
    }
    public void ProcessReceived(IMyLaserAntenna ant, string msg) {
        var msgArr = msg.Split('|');
        if (msgArr.Length >= 3) {
            if (msgArr[2] == myID && !receivedData.Contains(msg)) {
                WriteToLog("Fresh private message processed");
                receivedData.Add(msg);
            } else if (msgArr[2] == "ALL" && !sentData.ContainsKey(msg)) {
                WriteToLog("Not sent global message processed");
                if (!receivedData.Contains(msg)) receivedData.Add(msg);
                BroadcastMessage(msg, new List<IMyLaserAntenna>() {ant});
            } else if (!sentData.ContainsKey(msg)) {
                WriteToLog("Fresh message processed");
                BroadcastMessage(msg, new List<IMyLaserAntenna>() {ant});
            }
        }
    }
    public void ProcessOnDisconnect(IMyLaserAntenna ant) { 
 
    }
    public void ProcessOnConnect(IMyLaserAntenna ant) {
        var sentDataKeys = new List<string>(sentData.Keys);
        for (var i = 0; i < sentDataKeys.Count; i++) {
            if ((DateTime.Now-sentData[sentDataKeys[i]]).Ticks/10000000 < oldMsgTimeout)
                BroadcastMessageData(ant, sentDataKeys[i]);
        }
    }
    void WriteToLog(string msg, string prefix = "N/Y") { 
        log += string.Format("{0} - {1}\n", prefix, msg); 
        var logArray = log.Substring(0, log.Length-1).Split('\n'); 
        var newLog = ""; 
        for (var i = Math.Max(0, logArray.Length-logLinesCount); i < logArray.Length; i++) { 
            newLog += logArray[i]+"\n"; 
        } 
        log = newLog; 
    }

    public class IGLC {
        List<string> dataToSend = new List<string>();
        List<string> dataReceived = new List<string>();
        MyGridProgram program;
        IGLC_Main parent;
        public string log = "";
        string logPanelName;
        int logLinesCount = 15;
        IMyLaserAntenna antenna;
        bool antennaNotFound = false;
        bool antennaNotConn = false;
        bool passive = false;
        public string income;
        string[] init_sequence = new string[] {"11011011011011011", "11011010101011011"};
        int incomeHandler = 20;                             // must be higher than length of init_sequence
        bool receiving = false;
        bool transmitting = false;
        string receivingData = "";
        int receivingLength = -1;
        int answerIndex = 0;
        string receiveHolder = "";
        int checkIndex = 0;
        string rawDataToSend = "";
        int sendIndex = 0;
        int check2Index = 0;
        bool noVerification = false;

        public IGLC(MyGridProgram me, IGLC_Main iglc, string antennaName, string logLCDName = "") {
            program = me;
            parent = iglc;
            logPanelName = logLCDName;
            antenna = program.GridTerminalSystem.GetBlockWithName(antennaName) as IMyLaserAntenna;
            income = Multiply("0", incomeHandler);
            if (passive) mode = 0;
            else mode = 1;
            SendBit("0");
            me.Echo("Class initialized!");
        }
    
        /*
        =========================================================
            0 - passive (only ready to receive)
            1 - enabled (no available message to send)
            2 - sending (actual message is being sent)
            3 - receiving (listening for message)
            4 - answer (init sequence caught)
            5 - check receiver (check received data)
            6 - check sender (check sent data from receiver)
        =========================================================
        */
        int mode = 0;
        public void ProcessTick() {
            if (antenna == null) {
                if (!antennaNotFound) {
                    WriteToLog("Antenna not found! Aborting...","ERR");
                    antennaNotFound = true;
                }
            } else if (antennaNotFound && antenna != null) {
                WriteToLog("Antenna found.","INF");
                antennaNotFound = false;
            } else if (!isConnected()) {
                if (!antennaNotConn) {
                    WriteToLog("Antenna disconnected!","INF"); 
                    antennaNotConn = true;
                    parent.ProcessOnDisconnect(antenna);
                }
            } else if (isConnected() && antennaNotConn) {
                WriteToLog("Antenna connected","INF");  
                antennaNotConn = false;
                parent.ProcessOnConnect(antenna);
            } else { // ===> START - ANTENNA IS NOT NULL - CHECK
                income = income.Substring(1,Math.Min(income.Length-1,incomeHandler-1));
                var currBit = GetBit().ToString();
                income += currBit;
                var substr = "";
                switch (mode) {
                    case 0:   // ----------------------------------------------------------------------------
                        substr = income.Substring(incomeHandler-init_sequence[noVerification ? 1 : 0].Length,init_sequence[noVerification ? 1 : 0].Length);
                        if (substr == init_sequence[0] || substr == init_sequence[1]) {
                            receiving = true;
                            mode = 4;
                            WriteToLog("Receiving mode activated", "INF");
                            if (substr == init_sequence[1]) { 
                                noVerification = true; 
                                WriteToLog("No verify mode!", "INF"); 
                            }
                        }
                        if (!income.Contains("0")) {
                            SendBit("0");
                            WriteToLog("Reseting stream to 0", "INF");
                        }
                        break;
                    case 1:    // ----------------------------------------------------------------------------
                        substr = income.Substring(incomeHandler-init_sequence[noVerification ? 1 : 0].Length,init_sequence[noVerification ? 1 : 0].Length); 
                        if (substr == init_sequence[0] || substr == init_sequence[1]) {
                            receiving = true; 
                            mode = 4;
                            WriteToLog("Receiving mode activated", "INF");
                            if (substr == init_sequence[1]) { 
                                noVerification = true; 
                                WriteToLog("No verify mode!", "INF");  
                            }
                        } else if (dataToSend.Count > 0 && !income.Contains("1")) {
                            rawDataToSend = String.Format("{0}{1}", Convert.ToString(dataToSend[0].Length, 2).PadLeft(8, '0'), 
                                StringToBinary(dataToSend[0]));
                            if (dataToSend[0][0] == '+') {
                                noVerification = true;
                                WriteToLog("No verify mode!", "INF");  
                            }
                            transmitting = true;
                            mode = 2;
                            SendBit("1");                                  // lock down, so other antenna will not start sending
                            WriteToLog("Transmitting mode activated, msg:\n <- "+dataToSend[0], "INF");
                        }
                        if (!income.Contains("0")) { 
                            SendBit("0"); 
                            WriteToLog("Reseting stream to 0", "INF"); 
                        }
                        break;
                    case 2:   // ----------------------------------------------------------------------------
                        if (sendIndex < init_sequence[noVerification ? 1 : 0].Length) {
                            SendBit(init_sequence[noVerification ? 1 : 0][sendIndex].ToString());
                        } else if (sendIndex > init_sequence[noVerification ? 1 : 0].Length && sendIndex < init_sequence[noVerification ? 1 : 0].Length*2+1) {
                            if (currBit != init_sequence[noVerification ? 1 : 0][(sendIndex-init_sequence[noVerification ? 1 : 0].Length-1)].ToString()) {
                                WriteToLog("Failed init sequence bit!", "ERR");
                                SendBit("0");
                                sendIndex = 0;
                                transmitting = false;
                                rawDataToSend = "";
                                noVerification = false;
                                mode = (passive ? 0 : 1);
                                break;
                            }
                        } else if (sendIndex >= init_sequence[noVerification ? 1 : 0].Length*2+1) {
                                if (sendIndex == init_sequence[noVerification ? 1 : 0].Length*2+1) WriteToLog("Sending data", "INF");
                                var dataIndex = sendIndex-init_sequence[noVerification ? 1 : 0].Length*2-1;
                                if (dataIndex >= rawDataToSend.Length) {
                                    if (noVerification) {
                                        WriteToLog("Sending completed", "INF");
                                        dataToSend.RemoveAt(0);
                                        sendIndex = 0; 
                                        rawDataToSend = ""; 
                                        mode = 6;
                                        transmitting = false;
                                        noVerification = false;
                                        mode = (passive ? 0 : 1);
                                        break;
                                    } else {
                                        WriteToLog("Nothing to send, next run in mode 6", "INF");
                                        sendIndex = 0;
                                        rawDataToSend = "";
                                        mode = 6;
                                        break;
                                    }
                                }
                                SendBit(rawDataToSend[dataIndex].ToString());
                        }
                        sendIndex++;
                        break;
                    case 3:   // ----------------------------------------------------------------------------
                        receivingData += currBit;
                        if (receivingLength == -1 && receivingData.Length == 8) {
                            receivingLength = Convert.ToInt32(receivingData, 2);
                            receivingData = "";
                            WriteToLog("8th bit received, size: "+receivingLength.ToString(), "INF");
                        } else if (receivingLength > -1) {
                            if (receivingData.Length >= receivingLength*8) {
                                if (noVerification) {
                                    receivingData = receivingData.Substring(0, Math.Min(receivingData.Length, receivingLength*8));
                                    WriteToLog("Data received:\n -> "+BinaryToString(receivingData), "INF");
                                    dataReceived.Add(receivingData);
                                    parent.ProcessReceived(antenna, BinaryToString(receiveHolder));
                                    SendBit("0");
                                    receivingData = ""; 
                                    receivingLength = -1;
                                    receiving = false;
                                    noVerification = false;
                                    mode = (passive ? 0 : 1);
                                } else {
                                    WriteToLog("Receiving complete, next mode 5", "INF");
                                    receivingData = receivingData.Substring(0, Math.Min(receivingData.Length, receivingLength*8));
                                    receiveHolder = receivingData;
                                    receivingData = "";
                                    receivingLength = -1;
                                    mode = 5;
                                }
                            }
                        }
                        break;
                    case 4:   // ----------------------------------------------------------------------------
                        if (answerIndex == init_sequence[noVerification ? 1 : 0].Length) { 
                            WriteToLog("Answer sent, next run receiving", "INF"); 
                            mode = 3; 
                            answerIndex = 0;
                            break;
                        }
                        SendBit(init_sequence[noVerification ? 1 : 0][answerIndex].ToString());
                        answerIndex++;
                        break;
                    case 5:   // ----------------------------------------------------------------------------
                        if (checkIndex > 0 && Invert(GetBit()).ToString() != receiveHolder[checkIndex-1].ToString()) {
                            WriteToLog("Check response mismatch!", "ERR");
                            SendBit("0");
                            checkIndex = 0;
                            receiving = false;
                            receiveHolder = "";
                            noVerification = false;
                            mode = (passive ? 0 : 1);
                            break;
                        }
                        if (checkIndex < receiveHolder.Length) {
                            if (checkIndex == 0) WriteToLog("Sending data to check", "INF");
                            SendBit(receiveHolder[checkIndex].ToString());
                        } else {
                            WriteToLog("Data succesfully received:\n -> "+BinaryToString(receiveHolder), "INF");
                            SendBit("0");
                            checkIndex = 0;
                            dataReceived.Add(receiveHolder);
                            parent.ProcessReceived(antenna, BinaryToString(receiveHolder));
                            receiving = false;
                            receiveHolder = "";
                            noVerification = false;
                            mode = (passive ? 0 : 1);
                            break;
                        }
                        checkIndex++;
                        break;
                    case 6:   // ----------------------------------------------------------------------------
                        rawDataToSend = StringToBinary(dataToSend[0]);
                        if (currBit == rawDataToSend[check2Index].ToString()) {
                            SendBit(Invert(GetBit()).ToString());
                        } else {
                            WriteToLog("Check income mismatch!", "ERR");
                            SendBit("0");
                            check2Index = 0;
                            rawDataToSend = "";
                            transmitting = false;
                            noVerification = false;
                            mode = (passive ? 0 : 1);
                            break;
                        }
                        check2Index++;
                        if (check2Index == rawDataToSend.Length) {
                            WriteToLog("Data succesfully sent", "INF");
                            check2Index = 0;
                            rawDataToSend = "";
                            dataToSend.RemoveAt(0);
                            transmitting = false;
                            noVerification = false;
                            mode = (passive ? 0 : 1);
                        }
                        break;
                    default:   // ----------------------------------------------------------------------------
                        WriteToLog("Unknown mode!", "ERR");
                        break;
                }

            } // ===> END - ANTENNA IS NOT NULL - CHECK
            var lcdLog = program.GridTerminalSystem.GetBlockWithName(logPanelName) as IMyTextPanel;
            if (lcdLog != null) updateIfNeeded(income+String.Format(" (S {0} : {1} R)\n", dataToSend.Count.ToString(), 
                dataReceived.Count.ToString())+log, lcdLog);
        }

        //========================================================¨
        public void SendMessage(string msg) {
            WriteToLog("New message queued", "INF");
            var verify = true;
            if (msg.Length > 1 && msg.Substring(0,1) == "+") verify = false;
            dataToSend.Add(msg);
        }
        void WriteToLog(string msg, string prefix = "N/Y") {
            log += string.Format("{0} - {1}\n", prefix, msg);
            var logArray = log.Substring(0, log.Length-1).Split('\n');
            var newLog = "";
            for (var i = Math.Max(0, logArray.Length-logLinesCount); i < logArray.Length; i++) {
                newLog += logArray[i]+"\n";
            }
            log = newLog;
        }
        int GetBit() {
            if (antenna == null) return 0;
            return Convert.ToInt16(antenna.IsPermanent);
        }
        void SendBit(string bit) {
            if (antenna == null) return;
            if ((antenna.IsPermanent ? "1" : "0") != bit) antenna.ApplyAction("isPerm");
        }
        int Invert(int bit) {
            return (bit == 1 ? 0 : 1);
        }
        public bool isConnected() { 
            return (antenna.DetailedInfo.Split('\n')[2].Contains("Connected")); 
        }
        string StringToBinary(string data) {
            StringBuilder sb = new StringBuilder();  
       
            for (int c = 0; c < data.Length; c++)  
            {  
                sb.Append(Convert.ToString(((int)data[c] < 256 ? (int)data[c] : 255), 2).PadLeft(8, '0'));  
            }  
            return sb.ToString();  
        } 
        string BinaryToString(string data) {  
            List<Byte> byteList = new List<Byte>();  
            for (int i = 0; i < data.Length; i += 8)
            {
                byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }
        string Multiply(string inp, int s) {
            var txt = "";
            for (var i = 0; i<s; i++) {
                txt += inp;
            }
            return txt;
        }
        bool updateIfNeeded(string text, IMyTextPanel pan) {
            if (pan.GetPublicText() != text) {
                pan.WritePublicText(text);
                return true;
            } else {
                return false;
            }
        }
    }
}

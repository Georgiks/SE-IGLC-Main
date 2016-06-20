// =====================================================================  
//                                       USER CONFIGURABLE SETTINGS  
// ===================================================================== 
 
string messagesLCD = "LCD Centre 5 Base"; 
string statusLCD = "LCD Centre 8 Base"; 
static string GridID = "BA"; 
bool advancedDisplay = true; 
 
// ===================================================================== 
//                                  END OF USER CONFIGURABLE SETTINGS 
// ===================================================================== 
 
IGLC_Main stream; 
DisplayInterface display; 
 
int idhead1; 
int idhead2; 
int[] idhead = new int[4]; 
int[] id1bar = new int[4];  
int[] id2bar = new int[4]; 
int[] id3bar = new int[4]; 
 
public Program() { 
    var fTime = DateTime.Now.Millisecond; 
    stream = new IGLC_Main(this, messagesLCD); 
    if (advancedDisplay) { 
        var panel = GridTerminalSystem.GetBlockWithName(statusLCD) as IMyTextPanel; 
        display = new DisplayInterface(panel, this.Runtime, 0.2f); 
        display.bgrColor = "G4"; 
        idhead1 = display.AddElementString("ANTENNA CONTROL:", new int[] {1,1}, "R"); 
        idhead2 = display.AddElementRectangle(new int[] {1,7}, new int[] {64,1}, 1, 0, "R"); 
        idhead[0] = display.AddElementString("", new int[] {1,10}, "Y"); 
        id1bar[0] = display.AddElementBar(0f, new int[] {10,18}, new int[] {60,4}); 
        id2bar[0] = display.AddElementBar(0f, new int[] {10,23}, new int[] {29,4});  
        id3bar[0] = display.AddElementBar(0f, new int[] {41,23}, new int[] {29,4},"B"); 
        idhead[1] = display.AddElementString("", new int[] {1,30}); 
        id1bar[1] = display.AddElementBar(0f, new int[] {10,38}, new int[] {60,4}); 
        id2bar[1] = display.AddElementBar(0f, new int[] {10,43}, new int[] {29,4}); 
        id3bar[1] = display.AddElementBar(0f, new int[] {41,43}, new int[] {29,4},"B"); 
        idhead[2] = display.AddElementString("", new int[] {1,50}); 
        id1bar[2] = display.AddElementBar(0f, new int[] {10,58}, new int[] {60,4}); 
        id2bar[2] = display.AddElementBar(0f, new int[] {10,63}, new int[] {29,4}); 
        id3bar[2] = display.AddElementBar(0f, new int[] {41,63}, new int[] {29,4},"B"); 
        idhead[3] = display.AddElementString("", new int[] {1,70}); 
        id1bar[3] = display.AddElementBar(0f, new int[] {10,78}, new int[] {60,4}); 
        id2bar[3] = display.AddElementBar(0f, new int[] {10,83}, new int[] {29,4}); 
        id3bar[3] = display.AddElementBar(0f, new int[] {41,83}, new int[] {29,4},"B"); 
    } 
    Echo("INIT>> "+Runtime.CurrentInstructionCount+"/"+Runtime.MaxInstructionCount+"\n"+  
        "Runtime:   "+(DateTime.Now.Millisecond-fTime) + " ms"); 
} 
 
void Main(string argument) { 
    var fTime = DateTime.Now.Millisecond; 
    if (argument != "") { 
        stream.BroadcastMessage("ALL", argument, false); 
        return; 
    } 
    var respActiveLst = stream.GetStatusActive(); 
    var panelHolder = GridTerminalSystem.GetBlockWithName(statusLCD) as IMyTextPanel; 
    if (advancedDisplay) { 
        for (var i = 0; i < 4; i++) { 
            if (i < respActiveLst.Count) { 
                var response = respActiveLst[i]; 
                var el1 = display.GetElement(idhead[i]) as DisplayInterface.DisplayElementString; 
                var el2 = display.GetElement(id1bar[i]) as DisplayInterface.DisplayElementBar; 
                var el3 = display.GetElement(id2bar[i]) as DisplayInterface.DisplayElementBar; 
                var el4 = display.GetElement(id3bar[i]) as DisplayInterface.DisplayElementBar; 
                el1.isVisible = true; 
                el2.isVisible = true; 
                el3.isVisible = true; 
                if (response.Verify) el4.isVisible = true; 
                el1.Update(text: response.Antenna.CustomName); 
                el2.Update(data: (float)response.TotalProgress); 
                el3.Update(data: (float)response.TransferProgress); 
                if (response.Verify) el4.Update(data: (float)response.CheckProgress); 
            } else { 
                display.GetElement(idhead[i]).isVisible = false; 
                display.GetElement(id1bar[i]).isVisible = false; 
                display.GetElement(id2bar[i]).isVisible = false; 
                display.GetElement(id3bar[i]).isVisible = false; 
            } 
        } 
        if ((display.GetPanel() == null || display.GetPanel() != panelHolder) && panelHolder != null) 
            display.SetPanel(panelHolder); 
        display.Process(); 
    } else if (panelHolder != null) { 
        var statusHolder = "Antenna Info:\n"; 
        for (var i = 0; i < respActiveLst.Count; i++) { 
            var r = respActiveLst[i]; 
            statusHolder += string.Format("> {0} - {4}\n{1}\n{2} {3}\n", r.Antenna.CustomName, 
                createBar(60,r.TotalProgress), createBar(28,r.TransferProgress), createBar(28,r.CheckProgress), 
                (r.Mode==1 ? "Receiving" : "Transmitting")); 
        } 
        panelHolder.SetValueFloat("FontSize", 1.1f); 
        panelHolder.WritePublicText( statusHolder ); 
    } 
    stream.ProcessTick(); 
    Echo("Instructions>> "+Runtime.CurrentInstructionCount+"/"+Runtime.MaxInstructionCount+"\n"+ 
        "Runtime:   "+(DateTime.Now.Millisecond-fTime) + " ms"); 
} 
 
public string createBar(int size, double data) { 
    var logic = Math.Min(Math.Max((int)((data)*size), 0), size); 
    return string.Format("[{0}{1}]", Multiply("|",logic), Multiply("'",size - logic)); 
} 
string Multiply(string inp, int s) {  
    var txt = "";  
    for (var i = 0; i<s; i++) {  
        txt += inp;  
    }  
    return txt;  
} 
 
class IGLC_Main { 
    string myID = GridID; 
    int updateTick = 1;                        // Every which tick update antenna list 
    int oldMsgTimeout = 5*60; 
    int logLinesCount = 10;
    Dictionary<string, string> logPanelTable = new Dictionary<string, string>() { 
            {"Laser Antenna Base", "LCD Centre Base"}, 
            {"Laser Antenna 2 Base", "LCD Centre 2 Base"} 
    }; 
    // ======================- NO MORE EDITING IN THIS CLASS -=========================

    Dictionary<IMyLaserAntenna, IGLC> antennaList = new Dictionary<IMyLaserAntenna, IGLC>(); 
    Dictionary<string, DateTime> sentData = new Dictionary<string, DateTime>(); 
    List<string> receivedData = new List<string>(); 
    MyGridProgram program; 
    public string log; 
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
        //program.Echo("Antennas: " + antennaList.Count); 
        if (runTick > 1000) runTick = 0; 
        runTick++; 
    } 
 
    public List<IGLC.StatusResponse> GetStatusActive() { 
        var lst = new List<IGLC.StatusResponse>(); 
        var antVals = new List<IGLC>(antennaList.Values); 
        for (var i = 0; i < antVals.Count; i++) { 
            if (antVals[i].receiving || antVals[i].transmitting) lst.Add(antVals[i].GetStatus()); 
        } 
        return lst; 
    } 
    public List<IGLC.StatusResponse> GetStatusAll() {  
        var lst = new List<IGLC.StatusResponse>();  
        var antVals = new List<IGLC>(antennaList.Values);  
        for (var i = 0; i < antVals.Count; i++) {  
            lst.Add(antVals[i].GetStatus());  
        }  
        return lst;  
    } 
    public void BroadcastMessage(string to, string text, bool noValidy = false, 
                                    List<IMyLaserAntenna> except = null) {  
        var toSend = string.Format("{4}|{0}_{1}|{2}|{3}", myID, messageID, to, text, (noValidy ? "+" : ""));  
        messageID++;  
        var dictElements = new List<IMyLaserAntenna>(antennaList.Keys);  
        for (var i = 0; i < dictElements.Count; i++) { 
            if (antennaList[dictElements[i]].isConnected()) { 
                if (except != null) { 
                    if (!except.Contains(dictElements[i])) { 
                        antennaList[dictElements[i]].SendMessage(toSend); 
                    } 
                } else antennaList[dictElements[i]].SendMessage(toSend); 
            } 
        }  
        sentData.Add(toSend, DateTime.Now); 
    } 
    public void BroadcastMessage(string text, List<IMyLaserAntenna> except = null) {   
        var toSend = text; 
        var dictElements = new List<IMyLaserAntenna>(antennaList.Keys);   
        for (var i = 0; i < dictElements.Count; i++) { 
            if (antennaList[dictElements[i]].isConnected()) { 
                if (except != null) {  
                    if (!except.Contains(dictElements[i])) {  
                        antennaList[dictElements[i]].SendMessage(toSend);  
                    }  
                } else antennaList[dictElements[i]].SendMessage(toSend); 
            } 
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
        program.GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(antennas, x => x.IsFunctional); 
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
    bool IsAlreadyReceived(string msg) { 
        var msgArr = msg.Split('|'); 
        for (var i = 0; i < receivedData.Count; i++) { 
            var recArr = receivedData[i].Split('|'); 
            if (recArr.Length > 3 && (recArr[1] == msgArr[1] && recArr[3] == msgArr[3])) return true; 
        } 
        return false; 
    } 
    public void ProcessReceived(IMyLaserAntenna ant, string msg) { 
        var msgArr = msg.Split('|'); 
        if (msgArr.Length >= 4) { 
            if (msgArr[2] == myID && !IsAlreadyReceived(msg)) { 
                WriteToLog("Fresh private message processed"); 
                receivedData.Add(msg); 
            } else if (msgArr[2] == "ALL" && !sentData.ContainsKey(msg)) { 
                WriteToLog("Not sent global message processed"); 
                if (!IsAlreadyReceived(msg)) receivedData.Add(msg); 
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
        int logLinesCount = 15; 
        int incomeHandler = 20;                             // must be higher than length of init_sequence's strings 
        string[] init_sequence = new string[] {"11011011011011011", "11011010101011011"}; 
        // ======================- NO MORE EDITING IN THIS CLASS -=========================

        List<string> dataToSend = new List<string>(); 
        List<string> dataReceived = new List<string>(); 
        List<string> dataFailed = new List<string>(); 
        List<string> dataSent = new List<string>(); 
        MyGridProgram program; 
        IGLC_Main parent; 
        public string log = ""; 
        string logPanelName;
        IMyLaserAntenna antenna; 
        bool antennaNotFound = false; 
        bool antennaNotConn = false; 
        bool passive = false; 
        string income; 
        public bool receiving = false; 
        public bool transmitting = false; 
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
            //me.Echo("Class initialized!"); 
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
                                    parent.ProcessReceived(antenna, BinaryToString(receivingData)); 
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
                        if (check2Index == 0) rawDataToSend = StringToBinary(dataToSend[0]); 
                        if (currBit == rawDataToSend[check2Index].ToString()) { 
                            SendBit(Invert(GetBit()).ToString()); 
                        } else { 
                            WriteToLog("Check income mismatch!", "ERR"); 
                            SendBit(currBit); 
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
            if (!dataToSend.Contains(msg)) dataToSend.Add(msg); 
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
        public StatusResponse GetStatus() { 
            return new StatusResponse(this); 
        } 
        public class StatusResponse {  
            public long Time; 
            public bool Connected; 
            public int Mode; 
            public bool Verify = true; 
            public int ReceiveLength; 
            public double TotalProgress; 
            public double TransferProgress; 
            public double CheckProgress; 
            public string RawIncome; 
            public List<string> ReceivedList; 
            public List<string> SentList; 
            public List<string> ToSendList; 
            public List<string> FailedReceivedList; 
            public IMyLaserAntenna Antenna; 
            public StatusResponse(IGLC parent) { 
                Time = DateTime.Now.Ticks; 
                Mode = (parent.receiving ? 1 : (parent.transmitting ? 2 : 0)); 
                Connected = !parent.antennaNotConn; 
                Verify = !parent.noVerification; 
                ReceiveLength = parent.receivingLength; 
 
                TransferProgress = 0;  
                CheckProgress = 0; 
                if (Mode == 1) {            // receiving 
                    if (ReceiveLength == -1)  
                        if (parent.receiveHolder != "") TransferProgress = 1; 
                        else TransferProgress = parent.receivingData.Length/8f; 
                    else TransferProgress = parent.receivingData.Length/(ReceiveLength*8f); 
                    if (Verify && parent.receiveHolder.Length != 0) { 
                        CheckProgress = parent.checkIndex/(float)(parent.receiveHolder.Length); 
                    } 
                } else if (Mode == 2) {     // transmitting 
                    if (parent.mode == 2) TransferProgress = (float)(parent.sendIndex)/(parent.rawDataToSend.Length+ 
                                            parent.init_sequence[Verify ? 0 : 1].Length*2+1); 
                    else TransferProgress = 1; 
                    if (Verify && parent.rawDataToSend.Length != 0) { 
                        CheckProgress = (float)parent.check2Index/parent.rawDataToSend.Length; 
                    } 
                } 
                TotalProgress = (Verify ? (TransferProgress + CheckProgress)/2f : TransferProgress); 
                ReceivedList = parent.dataReceived; 
                ToSendList = parent.dataToSend; 
                SentList = parent.dataSent; 
                FailedReceivedList = parent.dataFailed; 
                RawIncome = parent.income; 
                Antenna = parent.antenna; 
            }  
        } 
    } 
} 
 
 
class DisplayInterface {  
    public string bgrColor = "G4";  
    int updateTick = 5; 
    List<string> grayList = new List<string>() {"'   '","!|!|!","\uE00F","\uE009","\uE00D","\uE00E","\uE006"};
    // ======================- NO MORE EDITING IN THIS CLASS -=========================
    // ==============- (you can however add new characters to convertTable -==================

    static Dictionary<char, List<List<int>>> convertTable = new Dictionary<char, List<List<int>>>();  
    Dictionary<string, string> colorTable = new Dictionary<string, string>();    
    public Dictionary<int, DisplayElement> elementList = new Dictionary<int, DisplayElement>();  
    string[] bgrArray;  
    int id_index = 0;  
    IMyTextPanel panel;  
    MyGridProgram meDebug;  
    IMyGridProgramRuntimeInfo runtime;  
    int x;  
    int y;  
    Single fontSize;  
    List<List<string>> data = new List<List<string>>(); 
    int tick = 0; 
  
    // Anti script-complexity system: should prevent "Script is too complex" error to be raised, instead it will  
    //   split the execution of script in several steps (speed of update will be decreased)  
    int ASCS_ind = 0;       // if is higher than 0, it means that last run was complex and we need to finish it.  
    List<DisplayElement> ASCS_lst = null;   // save sorted list to save Instructions  
  
    public DisplayInterface(IMyTextPanel pan, IMyGridProgramRuntimeInfo rt, Single fSize = (Single)0.2, int sizeX = 80, int sizeY = 89) {  
        panel = pan;  
        fontSize = fSize;  
        x = sizeX;  
        y = sizeY;  
        runtime = rt;  
  
        Initialize();  
        ResetData();  
    }  
  
    public DisplayInterface(IMyTextPanel pan, MyGridProgram me, IMyGridProgramRuntimeInfo rt, Single fSize = (Single)0.2, int sizeX = 80,   
                                    int sizeY = 89) {  
        panel = pan;  
        meDebug = me;  
        fontSize = fSize;   
        x = sizeX;   
        y = sizeY;  
        runtime = rt;  
        me.Echo("Debug ON");  
  
        Initialize();  
        ResetData();  
    }  
  
    public void Initialize() {  
        convertTable.Add(' ', textToBlock("000/000/000/000/000"));   
        convertTable.Add('A', textToBlock("010/101/111/101/101"));    
        convertTable.Add('B', textToBlock("110/101/110/101/110"));    
        convertTable.Add('C', textToBlock("010/101/100/101/010"));    
        convertTable.Add('D', textToBlock("110/101/101/101/110"));   
        convertTable.Add('E', textToBlock("111/100/110/100/111"));   
        convertTable.Add('F', textToBlock("111/100/110/100/100"));   
        convertTable.Add('G', textToBlock("011/100/101/101/011"));   
        convertTable.Add('H', textToBlock("101/101/111/101/101"));   
        convertTable.Add('I', textToBlock("111/010/010/010/111"));   
        convertTable.Add('J', textToBlock("001/001/001/101/111"));   
        convertTable.Add('K', textToBlock("101/101/110/101/101"));   
        convertTable.Add('L', textToBlock("100/100/100/100/111"));   
        convertTable.Add('M', textToBlock("101/111/101/101/101"));   
        convertTable.Add('N', textToBlock("111/101/101/101/101"));   
        convertTable.Add('O', textToBlock("010/101/101/101/010"));   
        convertTable.Add('P', textToBlock("110/101/110/100/100"));   
        convertTable.Add('Q', textToBlock("010/101/101/111/011"));   
        convertTable.Add('R', textToBlock("110/101/110/101/101"));   
        convertTable.Add('S', textToBlock("011/100/010/001/110"));   
        convertTable.Add('T', textToBlock("111/010/010/010/010"));   
        convertTable.Add('U', textToBlock("101/101/101/101/111"));   
        convertTable.Add('V', textToBlock("101/101/101/010/010"));   
        convertTable.Add('W', textToBlock("101/101/101/111/101"));   
        convertTable.Add('X', textToBlock("101/101/010/101/101"));   
        convertTable.Add('Y', textToBlock("101/101/010/010/010"));   
        convertTable.Add('Z', textToBlock("111/001/010/100/111"));   
  
        convertTable.Add('a', textToBlock("000/111/001/111/111"));    
        convertTable.Add('b', textToBlock("100/111/101/101/111"));    
        convertTable.Add('c', textToBlock("000/111/100/100/111"));    
        convertTable.Add('d', textToBlock("001/111/101/101/111"));    
        convertTable.Add('e', textToBlock("000/111/111/100/111"));    
        convertTable.Add('f', textToBlock("011/010/111/010/010"));    
        convertTable.Add('g', textToBlock("000/111/111/001/111"));    
        convertTable.Add('h', textToBlock("100/111/101/101/101"));    
        convertTable.Add('i', textToBlock("010/000/010/010/111"));    
        convertTable.Add('j', textToBlock("010/000/010/010/110"));    
        convertTable.Add('k', textToBlock("100/100/101/110/101"));    
        convertTable.Add('l', textToBlock("010/010/010/010/001"));    
        convertTable.Add('m', textToBlock("000/101/111/101/101"));    
        convertTable.Add('n', textToBlock("000/111/101/101/101"));    
        convertTable.Add('o', textToBlock("000/111/101/101/111"));    
        convertTable.Add('p', textToBlock("000/111/101/111/100"));    
        convertTable.Add('q', textToBlock("000/111/101/111/001"));    
        convertTable.Add('r', textToBlock("000/111/100/100/100"));    
        convertTable.Add('s', textToBlock("000/011/110/011/110"));    
        convertTable.Add('t', textToBlock("010/111/010/010/011"));    
        convertTable.Add('u', textToBlock("000/101/101/101/111"));    
        convertTable.Add('v', textToBlock("000/101/101/101/010"));    
        convertTable.Add('w', textToBlock("000/101/101/111/111"));    
        convertTable.Add('x', textToBlock("000/101/101/010/101"));    
        convertTable.Add('y', textToBlock("000/101/111/001/111"));    
        convertTable.Add('z', textToBlock("000/111/011/110/111"));   
   
        convertTable.Add('0', textToBlock("111/101/101/101/111"));   
        convertTable.Add('1', textToBlock("010/110/010/010/111"));   
        convertTable.Add('2', textToBlock("111/001/111/100/111"));   
        convertTable.Add('3', textToBlock("111/001/011/001/111"));   
        convertTable.Add('4', textToBlock("101/101/111/001/001"));   
        convertTable.Add('5', textToBlock("111/100/111/001/111"));   
        convertTable.Add('6', textToBlock("111/100/111/101/111"));   
        convertTable.Add('7', textToBlock("111/001/001/001/001"));   
        convertTable.Add('8', textToBlock("111/101/111/101/111"));   
        convertTable.Add('9', textToBlock("111/101/111/001/111"));   
   
        convertTable.Add('.', textToBlock("000/000/000/000/010"));   
        convertTable.Add(',', textToBlock("000/000/000/010/100"));   
        convertTable.Add('!', textToBlock("010/010/010/000/010"));   
        convertTable.Add('?', textToBlock("110/010/011/000/010"));   
        convertTable.Add('_', textToBlock("000/000/000/000/111"));   
        convertTable.Add(':', textToBlock("000/010/000/010/000"));   
        convertTable.Add('"', textToBlock("101/101/000/000/000"));   
        convertTable.Add('-', textToBlock("000/000/111/000/000"));   
        convertTable.Add('+', textToBlock("000/010/111/010/000"));   
        convertTable.Add('*', textToBlock("010/111/111/010/000"));   
        convertTable.Add('%', textToBlock("101/001/010/100/101"));   
        convertTable.Add('/', textToBlock("001/010/010/100/100"));   
        convertTable.Add('\\', textToBlock("100/010/010/001/001"));   
        convertTable.Add('>', textToBlock("100/010/001/010/100"));   
        convertTable.Add('<', textToBlock("001/010/100/010/001"));   
        convertTable.Add('\'', textToBlock("010/010/000/000/000"));   
        convertTable.Add('(', textToBlock("001/010/010/010/001"));   
        convertTable.Add(')', textToBlock("100/010/010/010/100"));   
        convertTable.Add(';', textToBlock("000/010/000/010/100"));   
        convertTable.Add('=', textToBlock("000/111/000/111/000"));   
        convertTable.Add('[', textToBlock("011/010/010/010/011"));   
        convertTable.Add(']', textToBlock("110/010/010/010/110"));   
        convertTable.Add('{', textToBlock("011/010/100/010/011"));   
        convertTable.Add('}', textToBlock("110/010/001/010/110"));   
        convertTable.Add('^', textToBlock("010/101/000/000/000"));  
  
        colorTable.Add("W", "\uE006");      // white  
        colorTable.Add("G1", "\uE00E");      // light gray  
        colorTable.Add("G2", "\uE00D");     // darker gray  
        colorTable.Add("G3", "\uE009");     // even darker gray  
        colorTable.Add("G4", "\uE00F");     // even darker gray  
        colorTable.Add("G5", "!|!|!");            // even darker gray  
        colorTable.Add("G6", "'   '");            // darkest gray  
        colorTable.Add("G", "\uE001");        // green  
        colorTable.Add("B", "\uE002");       // blue  
        colorTable.Add("R", "\uE003");       // red  
        colorTable.Add("Y", "\uE004");       // yellow  
    }  
  
    public void Process() { 
        tick++; 
        if (tick >= updateTick) { 
            Show(); 
            tick = 0; 
        } 
    }  
  
    public void RefreshData() {  
        if (ASCS_ind == 0)   
            ResetData();  
  
        var keys = new List<int>(elementList.Keys);  
        var layerElements = new List<DisplayElement>();  
        if (ASCS_lst != null) layerElements = ASCS_lst;  
        else {  
  
            for (var i = 0; i < keys.Count; i++) {  
                if (!elementList[keys[i]].isVisible) continue;  
                var layer = elementList[keys[i]].eLayer;  
                var index = 0;  
                for (var el = 0; el < layerElements.Count; el++) {  
                    if (layer <= layerElements[el].eLayer) index = el+1;  
                    else break;  
                }  
                layerElements.Insert(index, elementList[i]);  
            }  
        }  
        var ASCS_mergeLogic = 0;  
        for (var i = ASCS_ind; i < layerElements.Count; i++) {  
            ASCS_mergeLogic = (MergeData(layerElements[i]) ? 1 : 2);  
            if (ASCS_mergeLogic==2) {break;}  
            ASCS_ind = i;  
        }  
        if (ASCS_mergeLogic==2) {  
            ASCS_lst = layerElements;  
            return;  
        }  
        ASCS_lst = null;  
        ASCS_ind = 0;  
    }  
    public void RefreshScreen(string strData = "") {  
        if (strData == "") strData = ConvertData();  
        if (panel != null) { 
            panel.WritePublicText(strData); 
        } else if (meDebug != null) meDebug.Echo("No valid text panel");  
    }  
  
    public void Show() { 
        if (panel != null) panel.SetValueFloat("FontSize", fontSize); 
        RefreshData(); 
        if (ASCS_ind == 0) {  
            var toShow = ConvertData(); 
            if (panel.GetPublicText() != toShow) RefreshScreen(toShow);  
            else if (meDebug != null) meDebug.Echo("Text already shown");  
        } 
    }  
  
    public void ResetData() {  
        bgrArray = new string[x];  
        var color = colorTable[bgrColor];   
        for (var i = 0; i < x; i++) {  
            bgrArray[i] = color;  
        }  
        var newData = new List<List<string>>();   
        for (var ty = 0; ty < y; ty++) {  
            newData.Add(new List<string>(bgrArray));  
        }  
        data = newData;  
    }  
  
    public string ConvertData(List<List<string>> dat = null) {  
        if (dat == null) dat = data;  
        StringBuilder holder = new StringBuilder();  
        for (var row = 0; row < dat.Count; row++) {  
            holder.Append(string.Join("", dat[row]));  
            holder.Append("\n");  
        } 
        string holder2 = holder.ToString().Substring(0, holder.Length-1); 
        return holder2;  
    } 
  
    bool MergeData(DisplayElement what) {  
        if (runtime.CurrentInstructionCount > 49800) return false;  
        var ccTable = what.colorConvertTable;  
        var iposx = what.posX;  
        var iposy = what.posY;  
        var dat = what.eData;  
  
        for (var iy = 0; iy < dat.Count; iy++) {  
            for (var ix = 0; ix < dat[iy].Count; ix++) {  
                if ((iy+iposy) < y && (iy+iposy) >= 0 && (ix+iposx) < x && (ix+iposx) >= 0) {  
                    var d = dat[iy][ix];  
                    if (d != 0) {  
                        data[iy+iposy][ix+iposx] = (ccTable.ContainsKey(d) && colorTable.ContainsKey(ccTable[d]) ?  
                                colorTable[ccTable[d]] : colorTable["W"]);  
                    }  
                }  
                if (runtime.CurrentInstructionCount > 49800) return false;  
            }  
        }  
        return true;  
    }  
  
    public List<List<int>> textToBlock(string inp) {    
        var blck = new List<List<int>>();    
        foreach (var line in inp.Split('/')) {    
            var tmp = new List<int>();    
            var sp = line.ToCharArray();    
            for (var cha = 0; cha < sp.Length; cha++) {    
                var charac = sp[cha];    
                var x = 0;    
                Int32.TryParse(charac.ToString(), out x);    
                tmp.Add(x);    
            }    
            blck.Add(tmp);    
        }    
        return blck;    
    } 
    public IMyTextPanel GetPanel() { 
        return panel; 
    } 
    public void SetPanel(IMyTextPanel pan) { 
        panel = pan; 
    } 
    string Multiply(string what, int cnt) {  
        string holder = "";  
        for (var i = 0; i < cnt; i++) {  
            holder += what;  
        }  
        return holder;  
    }  
    public int AddElementString(string data, int[] position, string color="Y", int layer=0, string bCol="") {  
        var id = id_index;  
        elementList.Add(id,   
                new DisplayElementString(meDebug, data, position, color, layer, bCol) as DisplayElement);  
        id_index++;  
        return id;  
    }  
    public int AddElementBar(float data, int[] pos, int[] size, string color="G", int layer=0, string fCol="G1",  
                            float lvlColor2 = 2f, float lvlColor3 = 2f, string color2 = "Y", string color3 = "R") {   
        var id = id_index;   
        elementList.Add(id,    
                new DisplayElementBar(meDebug, data, pos, size, color, layer, fCol, lvlColor2,lvlColor3,color2,color3)  
                    as DisplayElement);   
        id_index++;   
        return id;   
    }  
    public int AddElementRectangle(int[] pos, int[] size, int thickness, int layer=0, string frameColor="Y",  
             string fillColor="") {    
        var id = id_index;    
        elementList.Add(id,   
            new DisplayElementRectangle(meDebug, pos, size, thickness, layer, frameColor, fillColor)   
                    as DisplayElement);    
        id_index++;    
        return id;    
    }  
    public int AddElementCircle(int[] pos, int[] size, int thickness, int layer=0, string frameColor="Y",   
             string fillColor="") {  
        var id = id_index;  
        elementList.Add(id,  
            new DisplayElementCircle(meDebug, pos, size, thickness, layer, frameColor, fillColor)  
                    as DisplayElement);  
        id_index++;  
        return id;  
    }  
    public int AddElementCustom(int[] pos, string data, Dictionary<int, string> cTable, int layer=0) {  
        var id = id_index;  
        elementList.Add(id,  
            new DisplayElementCustom(meDebug, pos, data, cTable, layer) as DisplayElement);  
        id_index++;  
        return id;  
    }  
    public void RemoveElement(int uid) {   
        DisplayElement holder = null;   
        if (elementList.ContainsKey(uid)) holder = elementList[uid];   
        elementList.Remove(uid);            // Hope Garbage Collector will work!  
    }  
    public DisplayElement GetElement(int uid) {  
        DisplayElement holder = null;  
        if (elementList.ContainsKey(uid)) holder = elementList[uid];  
        return holder;  
    }  
  
    public class DisplayElement {  
        public List<List<int>> eData;  
        public int posX;  
        public int posY;  
        public int eLayer;  
        public string eType;  
        public bool isVisible = true;  
        public Dictionary<int, string> colorConvertTable;  
        internal MyGridProgram me;  
  
        public DisplayElement(MyGridProgram m, string type,  int[] position, int layer = 0) {  
            me = m;  
            posX = position[0];  
            posY = position[1];  
            eLayer = layer;  
            eType = type;  
        }  
        public virtual void Refresh() {  
        }  
    }  
  
    public class DisplayElementString : DisplayElement {  
        public string tColor;  
        public string bColor;  
        string eText;  
  
        public DisplayElementString(MyGridProgram m, string dat, int[] position, string colorText = "Y",   
                        int layer = 0, string bgrdColor = "")   
                                : base(m, "TEXT", position, layer) {  
            tColor = colorText;  
            bColor = bgrdColor;  
            colorConvertTable = new Dictionary<int, string>() {{1, tColor},{2, bColor}};  
            eText = dat;  
  
            eData = ConvertText(dat);  
        }  
  
        public List<List<int>> ConvertText(string str) {   
            int lineSpace = 2;                                 // how many rows between lines   
   
            int logic = 0;   
            List<List<int>> lst = new List<List<int>>();   
            var strArr = str.Split('\n');  
            for (var line = 0; line < strArr.Length; line++) {   
                for (var row = (5+lineSpace)*line; row < 5*(line+1)+lineSpace*line; row++) {   
                    lst.Add(new List<int>());   
                    for (var letter = 0; letter < strArr[line].Length; letter++) {   
                        List<List<int>> letterData;   
                        if (new List<char>(convertTable.Keys).Contains(strArr[line][letter]))   
                            letterData = convertTable[strArr[line][letter]];   
                        else letterData = convertTable[' '];   
                        for (var i = 0; i < letterData[logic].Count; i++) {   
                            if (letterData[logic][i] == 1) lst[row].Add(1);  
                            else if (bColor != "") lst[row].Add(2);  
                            else lst[row].Add(0);  
                        }   
                        lst[row].Add(bColor != "" ? 2 : 0);                       // space between letters   
                    }   
                    logic = ((logic+1)%5);   
                }   
                if (line < strArr.Length-1) for (var br = 0; br < lineSpace; br++) {   
                    lst.Add(new List<int>());   
                }   
            }   
            return lst;   
        }  
        public void Update(string text="", string textColor="", string backColor=null) {  
            if (textColor!="") tColor = textColor;  
            if (backColor!=null) bColor = backColor;  
            if (text!="") {eData=ConvertText(text); eText=text;}  
            colorConvertTable = new Dictionary<int, string>() {{1, tColor},{2, bColor}};  
        }  
        public override void Refresh() {  
            eData=ConvertText(eText);  
        }  
        public string GetText() {  
            return eText;  
        }  
    }  
    public class DisplayElementBar : DisplayElement {   
        public string pColor;  
        public string fColor;  
        public string pColor2;  
        public string pColor3;  
        public float Color2;  
        public float Color3;  
        public int sizeX;  
        public int sizeY;  
        float progress;  
  
        public DisplayElementBar(MyGridProgram m, float dat, int[] position, int[] size, string color = "G",    
                        int layer = 0, string frameColor = "Y", float lvlColor2 = 2f, float lvlColor3 = 2f, string color2 = "Y",  
                        string color3 = "R")   
                                                : base(m, "BAR", position, layer) {   
            sizeX = size[0];  
            sizeY = size[1];  
            pColor = color;  
            fColor = frameColor;  
            pColor2 = color2;  
            pColor3 = color3;  
            Color2 = lvlColor2;  
            Color3 = lvlColor3;  
            progress = dat;  
            colorConvertTable = new Dictionary<int, string>() {{1, pColor},{2, fColor},{3, pColor2},{4, pColor3}};  
  
            eData = CreateProgressBar(dat);  
        }  
  
        public List<List<int>> CreateProgressBar(float data) {   
            List<List<int>> lst = new List<List<int>>();  
            var borderLine = new int[sizeX];  
            var barBody = new int[sizeX];  
            for (var i = 0; i < sizeX; i++) {  
                borderLine[i] = 2;  
  
                // I'm sorry for this, when I do nested condition variable via "if () {}", I get error "illegal one-byte branch"  
                // if i=first|last => 2; if i/max_i<data => (if i<color2_threshold => 1; if i< color3_threshold => 3; 4); 0  
                barBody[i] = ((i != 0 && i != sizeX-1) ? (((float)i)/(sizeX-2)<=data ? (((float)i)/(sizeX-2)<Color2 ? 1 :  
                    ((float)i)/(sizeX-2)<Color3 ? 3 : 4) : 0) : 2);  
            }  
  
            for (var iy = 0; iy < sizeY; iy++) {  
                if (iy==0 || iy==sizeY-1) {  
                    lst.Add(new List<int>(borderLine));  
                    continue;  
                } else lst.Add(new List<int>(barBody));  
            }  
            return lst;   
        }  
        public void Update(float data=-1.077f, string color1="", string color2="", string color3="", float lvlColor2=-1.077f,  
                        float lvlColor3=-1.077f, string frameColor="", int sizex=-1, int sizey=-1) {  
            // -1.077f to lower chance that we actually want to update to this number  
  
            if (data!=-1.077f) progress=data;  
            if (lvlColor2!=-1.077f) Color2 = lvlColor2;  
            if (lvlColor3!=-1.077f) Color3 = lvlColor3;  
            if (color1!="") pColor = color1;  
            if (color2!="") pColor2 = color2;  
            if (color3!="") pColor3 = color3;  
            if (frameColor!="") fColor = frameColor;  
            if (sizex!=-1) sizeX = sizex;  
            if (sizey!=-1) sizeY = sizey;  
            colorConvertTable = new Dictionary<int, string>() {{1, pColor},{2, fColor},{3, pColor2},{4, pColor3}};  
  
            eData = CreateProgressBar(progress);  
        }  
        public override void Refresh() {   
            eData=CreateProgressBar(progress);   
        }  
        public float GetProgress() {  
            return progress;  
        }  
    }  
    public class DisplayElementRectangle : DisplayElement {  
        public int sizeX;  
        public int sizeY;  
        public int thick;  
        public string fColor;  
        public string inColor;  
  
        public DisplayElementRectangle(MyGridProgram m, int[] position, int[] size, int thickness = 1, int layer = 0,  
                        string frameColor="Y", string fillColor = "")  
                                : base(m, "RECT", position, layer) {  
            sizeX = size[0];  
            sizeY = size[1];  
            thick = thickness;  
            fColor = frameColor;  
            inColor = fillColor;  
            colorConvertTable = new Dictionary<int, string>() {{1, fColor},{2, inColor}};  
  
            eData = DrawRectangle();  
        }  
        public List<List<int>> DrawRectangle() {  
            List<List<int>> lst = new List<List<int>>();  
            var borderLine = new int[sizeX];   
            var body = new int[sizeX];   
            for (var i = 0; i < sizeX; i++) {   
                borderLine[i] = 1;  
                body[i] = ((i >= thick && i < sizeX-thick) ? (inColor != "" ? 2 : 0) : 1);   
            }  
            for (var iy = 0; iy < sizeY; iy++) {  
                if (iy<thick || iy>=sizeY-thick) {  
                    lst.Add(new List<int>(borderLine));  
                    continue;  
                } else lst.Add(new List<int>(body));  
            }  
            return lst;  
        }  
        public void Update(int sizex=-1, int sizey=-1,int thickness=-1, string frameColor="", string fillColor=null) {  
            if (sizex!=-1) sizeX = sizex;  
            if (sizey!=-1) sizeY = sizey;  
            if (thickness!=-1) thick = thickness;  
            if (frameColor!="") fColor = frameColor;  
            if (fillColor!=null) inColor = fillColor;  
            colorConvertTable = new Dictionary<int, string>() {{1, fColor},{2, inColor}};  
  
            eData = DrawRectangle();  
        }  
        public override void Refresh() {  
            eData = DrawRectangle();  
        }  
    }  
    class DisplayElementCircle : DisplayElement {  
        int sizeX;  
        int sizeY;  
        int thick;  
        string fColor;  
        string inColor;  
          
        public DisplayElementCircle(MyGridProgram m, int[] position, int[] size, int thickness=1, int layer=0,  
                string color = "Y", string fillColor = "") : base(m, "CIRCLE", position, layer) {  
            thick = thickness;  
            sizeX = size[0];  
            sizeY = size[1];  
            fColor = color;  
            inColor = fillColor;  
            colorConvertTable = new Dictionary<int, string>() {{1, fColor},{2, inColor}};  
  
            eData = DrawCircle();  
        }  
        public List<List<int>> DrawCircle() {  
            List<List<int>> lst = new List<List<int>>();   
            var center = new float[] {sizeX/2f, sizeY/2f};  
            float a2 = (float)Math.Pow(sizeX/2f,2);  
            float b2 = (float)Math.Pow(sizeY/2f,2);  
            for (var iy = 0.5f; iy < sizeY; iy++) {  
                lst.Add(new List<int>());  
                for (var ix = 0.5f; ix < sizeX; ix++) {  
                    double length = (Math.Pow(ix-center[0], 2)/a2 + Math.Pow(iy-center[1], 2)/b2);  
                    double length_min = (Math.Pow(ix-center[0], 2)/Math.Pow(sizeX/2f-thick,2) +  
                        Math.Pow(iy-center[1], 2)/Math.Pow(sizeY/2f-thick,2));  
                    lst[(int)iy].Add(length < 1 ? (length_min >= 1 ? 1 : (inColor == "" ? 0 : 2)) : 0);  
                }  
            }  
            return lst;  
        }  
        public void Update(int sizex=-1, int sizey=-1,int thickness=-1, string frameColor="", string fillColor=null) {   
            if (sizex!=-1) sizeX = sizex;   
            if (sizey!=-1) sizeY = sizey;   
            if (thickness!=-1) thick = thickness;   
            if (frameColor!="") fColor = frameColor;   
            if (fillColor!=null) inColor = fillColor;  
            colorConvertTable = new Dictionary<int, string>() {{1, fColor},{2, inColor}};  
   
            eData = DrawCircle();   
        }  
        public override void Refresh() {   
            eData = DrawCircle();   
        }  
    }  
    class DisplayElementCustom : DisplayElement {  
  
        public DisplayElementCustom(MyGridProgram m, int[] position, string data, Dictionary<int, string> colTable,  
                        int layer=0 )  
                    : base(m, "CUSTOM", position, layer) {  
            colorConvertTable = colTable;  
            eData = ConvertCustomData(data);  
        }  
        List<List<int>> ConvertCustomData(string dat) {  
            var lst = new List<List<int>>();  
            var datArr = dat.Split('\n');  
            for (var i = 0; i < datArr.Length; i++) {  
                lst.Add(new List<int>());  
                for (var a = 0; a < datArr[i].Length; a++) {  
                    var holder = 0;  
                    Int32.TryParse(datArr[i][a].ToString(), out holder);  
                    lst[i].Add(holder);  
                }  
            }  
            return lst;  
        }  
        public void Update(string data=null, Dictionary<int, string> colorTable=null, List<List<int>> dataRaw=null) {  
            if (data != null) eData = ConvertCustomData(data);  
            if (colorTable != null) colorConvertTable = colorTable;  
            if (dataRaw!=null) eData = dataRaw;  
        }  
    }  
}
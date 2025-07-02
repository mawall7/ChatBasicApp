using NetworkServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    //TODO: implement this and implement correct processing (and printing for console). Parly written message should be printed from writebuffer again with a received message, console has to be cleared.  

   
    
    public class ProcessConsoleResponse : IInputProcessor
    {
        private readonly IUI _ui;
        private readonly IUIRenderer _consoleRenderer;
        public IChatCommunicator _chatCommunicator { get; }
        public event Action<string> Message;
        public ProcessConsoleResponse(IUI ui, IUIRenderer consoleRenderer, IChatCommunicator chatCommunicator)
        {
            this._ui = ui;
            this._consoleRenderer = consoleRenderer;
            this._chatCommunicator = chatCommunicator;
            

        }
        public async Task HandleInputAsync(string inputresult,  StringBuilder WriteBuffer) 
        {

            //if (MessageParser.IsQuit(inputresult))
            //{
            //    _ui.Output("You quit the chat session.", MessageType.General); //TODO: change. wrong! should quit token should be sent;
            //    //break;  //can't break quit check should be externalized!
            //}



            if (inputresult.Contains("<|EOM|>")) // On Enter input (send the message) 
            {
                string messageToSend = inputresult == "<|EOM|>" ? WriteBuffer.ToString() + "<|EOM|>" : inputresult;  //EOM means enter input or else sent quit message
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
                try
                {
                    await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None); //TODO: TimeOut
                    messageToSend = messageToSend.Replace("<|EOM|>", "");
                    
                    _ui.Output(System.Environment.NewLine +
                      $"Sent: {messageToSend}", MessageType.General);

                    //await WaitForAckAsync();
                    WriteBuffer.Clear();
                    _ui.Output("\nWrite another message:", MessageType.General);

                }
                catch (SocketException e)
                {

                    _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
                    _chatCommunicator.Dispose();
                    throw;
                }
            }

            if (inputresult.Length == 1) // == is not token
            {

                WriteBuffer.Append(inputresult);
                //send code for writing... 
              /*  _consoleRenderer.ReRender(inputresult, WriteBuffer); *///externalize this witch check for rerenderinput? or keep since it belongs to UI
                byte[] PrintingBytes = Encoding.UTF8.GetBytes("<|PRINT|>");
                try
                {
                    await _chatCommunicator.SendAsync(PrintingBytes, SocketFlags.None);
                }

                catch (SocketException e)
                {
                    _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
                    _chatCommunicator.Dispose();
                    throw;

                }
            }
        }
         
        public async Task ProcessFullMessage(string inputresult, StringBuilder WriteBuffer)
        {
            if (inputresult.Contains("<|EOM|>")) // TO DO: externalize if check 
            {
                var messageToSend = GetFullInputMessage(inputresult, WriteBuffer);
                var messageBytes = Encoding.UTF8.GetBytes(messageToSend);
                
                try
                {
                    await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None); //TODO: TimeOut
                    messageToSend = messageToSend.Replace("<|EOM|>", "");
                    _ui.Output(System.Environment.NewLine +   //TODO : possible change. move this to follow SRP (use event to just add messages but, print elsewhere) or interface for the same. 
                      $"Sent: {messageToSend}", MessageType.General);

                    //await WaitForAckAsync();
                    WriteBuffer.Clear();
                    _ui.Output("\nWrite another message:", MessageType.General);

                }
                catch (SocketException e)
                {

                    _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
                    _chatCommunicator.Dispose();
                    throw;
                }
            }
        }

        public async Task ProcessPrintMessage(string inputresult, StringBuilder WriteBuffer)
        {
            if (inputresult.Length == 1) // == is not token
            {

                WriteBuffer.Append(inputresult);
                //send code for writing... 
                /*  _consoleRenderer.ReRender(inputresult, WriteBuffer); *///externalize this witch check for rerenderinput? or keep since it belongs to UI
                byte[] PrintingBytes = Encoding.UTF8.GetBytes("<|PRINT|>");
                try
                {
                    await _chatCommunicator.SendAsync(PrintingBytes, SocketFlags.None);
                }

                catch (SocketException e)
                {
                    _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
                    _chatCommunicator.Dispose();
                    throw; // should not throw, new SocketException();

                }
            }
        }

        public string GetFullInputMessage(string inputresult, StringBuilder WriteBuffer)
        {
            string messageToSend = inputresult == "<|EOM|>" ? WriteBuffer.ToString() + "<|EOM|>" : inputresult;  //EOM means enter input or else sent quit message
            return messageToSend;

        }





    }

    public class ProcessNoneConsoleResponse 
    {
        private readonly IUI _ui;
        private readonly IUIRenderer _consoleRenderer;
        public IChatCommunicator _chatCommunicator { get; }

        public ProcessNoneConsoleResponse(IUI UI, IUIRenderer _consoleRenderer, IChatCommunicator _chatCommunicator)
        {
            _ui = _ui;
            _consoleRenderer = _consoleRenderer;
            _chatCommunicator = _chatCommunicator;

        }


        public async Task HandleInputAsync(string inputresult, StringBuilder WriteBuffer) 
        {
            string msg = _ui.ReadInput(); // TO DO: wpf handler can make sure to only execute this on return press so don't need that part (unlike for console) for writing this has be fixed in some way
            if (msg.Contains("<|PRINT|>")) //FIX : change this ?
            {
                msg = "<|PRINT|>";
            }
            if (msg.Contains("<|Quit|>"))
                throw new TaskCanceledException();

            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None);
        } 

    }
}

- Main purpose was to make a backend project that included testing of asyncronous methods. 
- Another purpose of this project was to explore implementations of socket in a local chat application,
  the project should be able to be integrated with a UI frontend of choice like (like WPF/ MAUI) or default to console.

The things that has to be handled are
- receiving messages from the socket through TCP protocoll.
- sending and writing messages.
- disconnects from the remote (gracefull/ ungracefull).
- ui handling (ChatCommunicator class).
  
Message handling has to be done correclty, due to that received data 
will be stored in the client receivedbuffer and will still be there until it is Received(),
but, if other code for instance in the UI-thread will be executed, meanwhile new messages are sent from remote,
there may be a delay untill next Receive() execution. Unlike if you are declaring receiving listening in a completely different task with Task.Run
(which is prefered). 

Using Poll and Available (This approach didn't work for my use case though and is done in usually more low level usecases.):

To not make await ReceiveAsync() block the thread completely 
until message is sent from remote, you may do a check with Available() and Poll() first. 

using Poll is also preferred (however there may be a delay, 
but it will be more accurate since only using Available() doesn't know if the remote has closed 
(it only checks the local buffer and it may have been a sent message but Recived wasnt called until after a few milliseconds(or longer) and meanwhile the remote have closed the connection. I will only know if the local buffer has data ready , which can refer to previouly unread messages/data, so you have to also use Poll and preferably if you want fast processing checks of incoming messages you have to use Task.Run. 

however when testing this manually(Poll was recommended approach by chatgpt and reminds of that chatgpt won't always present the best solutions(unless you ask for one provided you know the details anyway! , which may anyway not be clear when learning)  
, there will be a timing issue and non messages will be missunderstood as disconnects.
So the way to do it instead, is by not using Poll or Available and by simpy checking if receive is 0, which should not be 0 so it shouldnt be used, you should just await the response, and on gracefully disconnects on remote (with Socket.Close() or .ShutDown() the response will be returned as 0. and ungracefull remote disconnects will throw a Socket Exception so this will not
return a 0, but can easily by caught by try catch. 

Another thing I learnt more about was to use CancellationToken, when another thread is interrupted for instance if you press (F1) to quit,
then if (you have a token.Cancel() in the Main class or outer scope this will notice the thread if you send the token , and check for token.IsCacellationRequested)
on ungracefull however disconnects will give a SocketError(and can be handled like allready mentioned).

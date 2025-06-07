- Main purpose was to make an integrated sproject including testing project of asyncronous mothods.

- Another purpose of this project was to explore implementations of socket in a local chat application.
  
  General considerations about implementations that you may wondering : 
	- DRY : In some cases like in the server and client classes I wrote different implementations
            of the methods. It was intentionall to experiment and compare different solutions, but
			the final project should be consistent. 

1) The challenges needed to be handled writing the application were:

- receiving messages from the socket.
- sending
- handle disconnects from the remote.
- printing and processing messages through decoupled ui (console ui or WPF/ MAUI is your choice).

2) The testing challenges needed to be handled were:
	- implement a testing friendly environment 
	- set up the tests, mocks (pretty straitforward)
	- setting up asyncronous tests.

1) Message handling has to be done correclty, due to that received data 
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

however when testing this (which was recommended approach by chatgpt and reminds of that chatgpt wonät always present the best solutions(unless you ask for a best solution then it may work!)
, there will be a timing issue and non messages will be missunderstood as disconnects.
So the way to do it instead, is by not using Poll or Available and by simpy checking if receive is 0, which should not be 0 so it shouldnt be used, you should just await the response, and on gracefully disconnects on 
remote (with Socket.Close() or .ShutDown() the response will be returned as 0. and ungracefull remote disconnects will throw a Socket Exception so this will not
return a 0, but can easily by caught by try catch. 

A question that comes up is : how to handle remote crashes and disconnets. you can't detect crashes with Poll, it can only detect graceful disconnects. 

Another thing I lernt was to use CancellationToken, when another thread is interrupted for instance if you press (F1) to quit (I may move this code later but, for now that code is in the write method),
then if (you have a token.Cancel() in the Main class or outer scope this will notice the thread if you send the token , and check for token.IsCacellationRequested)
on ungracefull disconnects there will be a SocketError(and can be handled like allready mentioned).

2) Testing challenges may be due to issues that developers are unaware, because it really doesn't matter to the application itself,
   . Testing an continously running method in a loop is one example, let's say 
   this method runs in a while loop and is stopped from console through F1 key press. Then the method has a risk of running forever and 
   if Task is being used there is no way of stopt/killing it (and is not advisable anyway). One example was when making a test
   for testing starting the application: ChatApplication.Run(). The application will normally try to Connect and retry Connect to remote
   , so this test will run forever (if connection isn't mocked), and you may not want to test what happens after connection. so there are 2 options:
   ether you could mock the connection and make connection return an error after an await, to see that it runs. which is an okey approach 
   cause it will let you see that the application starts (stopping it afterwards by returning an error is okey since it is done in an controlled way)
   However you may not always want to return an error to stop the application, because you may want to test a method in isolation, and retuning errors may
   not work. So a solution in that case is to start the tested method in a Task and pass a CancellationTokenSource token. 
   This depends on that the method is implemented in this way. Most applicatins should be implemented for error handling, so that should always
   be in place, but developers may not be aware of implementing a Cancellation in this way. It was done in this project which is a good way for stopping
   a task gracefully in a test. In non async methods another way is to pass a Funct delegate in the method parameter that must be incuded
   in the program that can be invoked with true which will lead to breaking the loop.
   
   
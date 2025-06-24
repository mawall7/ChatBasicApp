- Main purpose was to make an integrated sproject including testing project of asyncronous mothods.

- Another purpose of this project was to explore implementations of socket in a local chat application.
  
  General considerations about implementations that you may wondering : 
	- DRY : In some cases like in the server and client classes I wrote different implementations
            of the methods. It was intentionall to experiment and compare different solutions, but
			the final project should be consistent. Later the 2 classes was merged into one, but,
			keeping the connection server client methods separation. I choose this option for 
			better readability in case the connect methods would grow in the future, 
			not fully DRY may be better than less readable code and less maintainability,
            if using external paratmeters. another option would be to create a connection base class
			or interface, that may be created in the future if differences of the methods increases,
			then new implementations would be easy to do and keeping the differences capsulated. 

1) The challenges needed to be handled writing the application were:

- receiving messages from the socket.
- sending
- handle disconnects from the remote.
- printing and processing messages through decoupled ui (console ui or WPF/ MAUI is your choice).
- handle asyncronous Tasks and testing it. 

2) The testing challenges needed to be handled were:
	- implement a testing-friendly environment 
	- set up the tests, mocks (pretty straitforward?)
	- setting up asyncronous tests unit tests.
    - integration tests.

If possible, run integration tests where you have multiple ChatPeer instances talking to each other*/

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

however when testing this (which was recommended approach by chatgpt and reminds of that chatgpt won't always present the best solutions (unless you ask for a best solution then it may work!)
, there will be a timing issue and non messages will be missunderstood as disconnects.
So the way to do it instead, is by not using Poll or Available and by simpy checking if receive is 0, which should not be 0 so it shouldnt be used, you should just await the response, and on gracefully disconnects on 
remote (with Socket.Close() or .ShutDown() the response will be returned as 0. and ungracefull remote disconnects will throw a Socket Exception so this will not
return a 0, but can easily by caught by try catch. 

A question that comes up is : how to handle remote crashes and disconnets. you can't detect crashes with Poll, it can only detect graceful disconnects. 

Another thing I lernt was to use CancellationToken, when another thread is interrupted for instance if you press (F1) to quit (I may move this code later but, for now that code is in the write method),
then if (you have a token.Cancel() in the Main class or outer scope this will notice the thread if you send the token , and check for token.IsCacellationRequested)
on ungracefull disconnects there will be a SocketError(and can be handled like allready mentioned).

related to message handling I used an event handler for clear separation of UI and ChatCommunicator class, however since the Listen and Write methods both are
using this, a racing condition may occur leading to interruptions, so a lock was used for the UI.  

2) 
    Setting up mocks in test I had done before, however not for for async methods.
	I tested the ChatPeer class and I figured these test should be included for basic functionality:
	important test cases: connect as server, connect as client, send and receive messages, what happens at disconnects?,
	what happens during failed connections?
	
   Testing challenges may be due to issues that developers are unaware of when writing code, because some cases may only be of test importance but,
   so it is important to think of writing test friendly code. Testing a continously running method in a loop is one example, let's say 
   this method runs in a while loop and is stopped from console through F1 key press. Then the method has a risk of running forever and 
   if Task is being used there is no way of stopt/killing it (and is not advisable anyway). One example was when making a test
   for testing starting the application: ChatApplication.Run(). The application will normally try to Connect and retry Connect to remote
   , so this test will run forever (if connection isn't mocked), and you may not want to test what happens after connection. so there are 2 options:
   ether you could mock the connection and make connection return an error after an await, to see that it runs. which is an okey approach 
   cause it will let you see that the application starts (stopping it afterwards by returning an error is okey since it is done in an controlled way)
   However you may not always want to return an error to stop the application, because you may want to test a method in isolation, and returning errors may
   not work. So a solution in that case is to start the tested method in a Task and pass a CancellationTokenSource token. 
   This depends on that the method is implemented in this way. Most applicatins should be implemented for error handling, so that should always
   be in place, but developers may not be aware of implementing a Cancellation for testing, I tried both stop a task by cancellation token and by just
   throwing an error (like SocketException) in this projects tests. 
   I had some proble with cancellationtokens, when a method tested has a tight loop , code execution may be to fast for cancellations to be observed,
   in this case you can setup the mock with setupsequence and return an exception after returning what you want to test, or you could make a delay in the
   tested method if possible during testing. I will look into why this did not work properly, it lead to a Null reference Exception.

   when writing tests for Cancelling the WriteAsync I have learnt from some misstakes. the test kept running infinitely, so it was suspected that the test
   was not cancelled by the cacellationtoken in the test. chatgpt made suggestions, that were missleading. It suggested, that a Task delay may interfering
   or that the inner task kept running cause the cancellation token was not passed to the that task, or that it was a thight loop and cancellation may never happen
   if it is ignored by the SendAsync method that is awaited. however these methods were mocked so they should return instantly. After debugging this method I 
   realized there was an uncaught NullException, stemming from that ReceiveInput had not been mocked. Even if ChatpGpt didn not give the solution it however 
   gave some tips on improvements for a more robust testing and system, like passing a CancellationToken to the inner method, which will support clean cancellation.
   You can also make the outer method catch OperationCancelledException to test this clean cancellation. 
   Another thing that testing this method made me realize was that I should have implemented a timeout for the chat ReadAsync and SendAsync operations from start,
   it's common practise for sending recieving async.

  - Integration tests

according to chatgpt:

you have:

✅ Clear separation of concerns

✅ Interfaces for UI and Network layers

✅ Pure tests on logic without external dependencies

✅ Cancellation tests

You could easily upgrade it to testable full integration by adding:

In-memory implementation of IChatCommunicator (for real message passing simulation)

In-memory IUI implementation (already done)

Write true end-to-end simulation tests — still fully in-process without real sockets or console

reflection on integration tests: 

Integration tests redirecting console input using Process are rarely used and might 
be flaky , because of timing issues that has to be handled and therefore hard to implement,
and also slow which is not good for CI/CD pipelines where you want tests to run fast and they 
can also be OS dependent. They are sometimes
included in end-to-end tests but, (I did some experimentation with one such test) but,
they will probably not be included in the final project, because of these reasons.  

Some chatgpt advice:

testing with inmemory ICommunicator (itegration test without real socket):  
Why is this still very useful?
Because most of the complexity in your application isn't in the socket layer — it’s in how you:

Handle protocols (your special markers: <|EOM|>, <|ACK|>, etc.)

Process messages

Control flow

UI updates

Error handling

If you can fully test that, you get:

✅ High confidence that your protocol is correct
✅ Fast tests (because there’s no real network)
✅ Deterministic tests (no flakiness from real sockets)
   
Error considerations and solution: 

SocketErrors : At some time I got a hang up of the application, but the cause were not some suspect
internal Socket Errors. I 
Checked chatgpt for these errors : DualMode system.not supported Exception, EnableBroadCast socketException
MultiCastLoopBack socketException. Those internal errors usually don’t interfer with using the socket and 
doesn't have to be handled, they happen just because you havn't opted for these settings. And I dind't have
to take them in consideration for the sake of my setup.

HangUps during ErrorHandling: After abstracting the socket methods into ChatCommunicator class, at some point 
there was a hang up of the server when manually testing to disconnect with ctrl-c. first I followed the error 
to the handler class which was related to remote socket Null Reference. Catching the NRE didn't make sense,
since that error didn't come up before and Socket Error should be the imediate error to catch after an ungracefull 
disconnect, so there had to be another issue. And this was right and a simple bug was causing this. 
There was still a Socket Exception that was being caught as intended, but a break statement was gone in the server
listen method, so the handler tried to access the socket after it had been disposed. After fixing this the application
was working and closing down again correctly. 
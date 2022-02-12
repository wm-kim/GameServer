using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace ServerCore
{
    class Listener
    {
        Socket _listenSocket;
        Func<Session> _sessionFactory; // 어떤 Session을 만들어줄지 정의

        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sessionFactory += onAcceptHandler;

            // 문지기 교육
            _listenSocket.Bind(endPoint);

            // 영업 시작
            // backlog : 최대 대기수
            _listenSocket.Listen(10);

            // 한번만 만들면 재사용 가능, 동시다발적으로 많은 유저를 받아야할 때 이부분을 for문을 걸어 늘려준다.
            SocketAsyncEventArgs args = new SocketAsyncEventArgs(); 
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args);
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            // SocketAsyncEventArgs 초기화, 기존의 잔재를 없앰
            args.AcceptSocket = null;

            bool pending = _listenSocket.AcceptAsync(args);
            if (pending == false) // 동시다발적으로 계속 false만 나오는 경우는 거의 없다.
                OnAcceptCompleted(null, args);
        }

        // 콜백함수는 별도의 Thread를 이용, ThreadPool에서 뽑아서 실행 main과 race condition이 일어나지 않도록 주의
        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success)
            {
                // 나중에 GameSession이 아니라 다른 Session이 될 수도 있어서 문제가 있다.
                // 어떤 Session을 만들건지 정해야함
                // GameSession session = new GameSession();
                Session session = _sessionFactory.Invoke();
                session.Start(args.AcceptSocket);
                // 여기서 다음으로 넘어가는 순간에 client에서 연결을 끊어버리면 밑에 라인에서 에러가난다.
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);
                // _onAcceptHandler.Invoke(args.AcceptSocket);
            }
            else 
                Console.WriteLine(args.SocketError.ToString());

            // stackoverflow가 일어나지 않는다.
            RegisterAccept(args);
        }
    }
}

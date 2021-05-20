﻿using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Servidor
{
    class Server
    {
        private IPAddress EnderecoIp;
        private int Porta;

        public static Hashtable Usuarios = new Hashtable(10);

        private TcpClient TcpServer = new TcpClient();

        public static EventHandler<StatusChangedEventArgs> StatusChanged;

        private Thread ThreadListener;

        private bool ServidorRodando = false;

        public Server(IPAddress enderecoIp, int porta)
        {
            EnderecoIp = enderecoIp;
            Porta = porta;
        }

        private TcpListener ListenerServidor;
        public void IniciarServidor()
        {
            ListenerServidor = new TcpListener(EnderecoIp, Porta);
            ListenerServidor.Start();

            ServidorRodando = true;

            ThreadListener = new Thread(ManterServidor);
            ThreadListener.IsBackground = true;
            ThreadListener.Start();
        }

        private void ManterServidor()
        {
            while (ServidorRodando)
            {
                try
                {
                    TcpServer = ListenerServidor.AcceptTcpClient();
                    ConexaoUsuario novaConexao = new ConexaoUsuario(TcpServer);
                }
                catch (Exception)
                {
                    EnderecoIp = null;
                    Porta = 0;
                }
            }
        }

        public static void ValidarMensagem(string usuario, string mensagem)
        {
            string tempMensagem = mensagem.Trim();
            if (tempMensagem != "")
            {
                EnviarMensagem(usuario, tempMensagem);
            }
        }

        private static void EnviarMensagem(string usuario, string mensagem)
        {
            StreamWriter mensagemUsuario;

            OnStatusChanged($"{usuario}: {mensagem}");

            foreach (TcpClient cliente in Usuarios.Values)
            {
                mensagemUsuario = new StreamWriter(cliente.GetStream());
                mensagemUsuario.WriteLine($"{usuario}: {mensagem}");
                mensagemUsuario.Flush();
            }
        }

        public static void EnviarMensagemAdmin(string mensagem)
        {
            StreamWriter mensagemAdmin;

            OnStatusChanged($"Adminstrador: {mensagem}");

            TcpClient[] tcpClients = new TcpClient[Usuarios.Count];
            Usuarios.Values.CopyTo(tcpClients, 0);

            foreach (TcpClient cliente in tcpClients)
            {
                if (mensagem.Trim() == "" || cliente == null)
                    continue;
                mensagemAdmin = new StreamWriter(cliente.GetStream());
                mensagemAdmin.WriteLine($"Administrador: {mensagem}");
                mensagemAdmin.Flush();
            }
        }

        public static void OnStatusChanged(string eventMessage)
        {
            if (StatusChanged != null)
            {
                StatusChanged(null, new StatusChangedEventArgs() { EventMessage = eventMessage });
            }
        }

        public void FecharServidor()
        {
            ServidorRodando = false;
            foreach (TcpClient usuario in Usuarios.Values)
            {
                usuario.Close();
            }

            ListenerServidor.Stop();
        }

        public static void CriarBackup(string texto)
        {
            string path = @$"C:\Users\{Environment.UserName}" +
                @$"\Desktop\Backup";

            if (Directory.Exists(path) == false)
            {
                DirectoryInfo di = Directory.CreateDirectory(path);
            }

            using (StreamWriter writer = File.CreateText($"{path}\\" +
                $"{DateTime.Now:dd-MM-yyyy HHmmss}.txt"))
            {
                writer.WriteLine(texto);
            }
        }
    }
}
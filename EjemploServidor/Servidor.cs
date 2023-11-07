using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace EjemploServidor  //esta namespace contiene no solo la clase servidor, si no las otras dos clases que son clases que representan argumentos de eventos
{                           
    // Declaro una subclase de EventArgs para los eventos del servidor
    public class ServidorEventArgs : EventArgs  //obviamente para ser argumentos de eventos tienen q heredar de EventArgs o algo que herede de una jerarquia EventArgs
    {
        public ServidorEventArgs(IPEndPoint ep)
        {

            //el constructuor permite recibir un objeto q tiene info. de un punto q representa un nodo de conexión

            //la info que tiene IPEndPoint es ip y puerto
            //encapsula la dirección de red y el puerto de un punto de servicio.

            EndPoint = ep;
        }

        //propiedades autoimplementadas pueden ser solo lectura
        public IPEndPoint EndPoint { get; } //retorna un ip endPoint
    }

    // Declaro una subclase de ServidorEventArgs específicamente para el evento DatosRecibidos
    public class DatosRecibidosEventArgs : ServidorEventArgs
    {
        public DatosRecibidosEventArgs(IPEndPoint ep, string datos) : base(ep)
        {
            //base esta llamando a la clase base q es servidoreventargs
            //llama a constructor de su clase base 

            DatosRecibidos = datos;
        }

        public string DatosRecibidos { get; set; }
    }

    public class Servidor
    {
        // Esta estructura permite guardar la información sobre un cliente
        private struct InfoDeUnCliente  
        {
            public Socket Socket; // Socket utilizado para mantener la conexión desde el servidor con cada uno de los clientes
            public Thread Thread; // Thread utilizado para poner la rutina que se queda escuchando a los clientes en un subproceso
                                  //porque en un proceso principal voy a correr el formulario del servidor y en otros subprocesos al servidor escuchando, o sea correr la "oreja" 

            //un subproceso para el formulario, otro para la oreja,  y n por cada cliente cliente conectado.
            //el socket en este caso permite mantener la conex con un cliente particular


            //estan dentro de una estructura, esta permite almacenar un conjunto de variables q representan un mismo concepto
            // (es un derivado de tipo abstracto de dato y es un poco mas restrictiva que las clases)


            //un socket para escuchar y un socket para cada envio y recepcion de datos,,uno del lado del servidor otro del cliente

        }
        //es distinto un struct en c y en c#
        //en C# son reference type como un objeto, clase ... , no es value type como en C)




        Thread listenerThread; // Thread de escucha de nuevas conexiones
        TcpListener listener; // Este objeto nos permite escuchar las conexiones entrantes
        //el objeto listener nos va a permitir establecer la oreja q escucha
        //listenerThread va a permitir correr un subproceso q ejecuta el codigo que permite ponerse en modo escucha


        // En este dictionary vamos a guardar la información de todos los clientes conectados.
        // ConcurrentDictionary se puede usar desde múltiples threads sin necesidad de locks.
        // Ver: https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2
        ConcurrentDictionary<IPEndPoint, InfoDeUnCliente> clientes = new ConcurrentDictionary<IPEndPoint, InfoDeUnCliente>();

        //EL SERVIDOR CADA CLIENTE Q SE CONECTA, TIENE Q TENER INFO. DE DONDE SE ESTA EJECUTANDO Y SU SOCKET


        //DICCIONARIO CONCURRENTE: TIENE VALOR KEY(NO SE REPITE) Y VALUE(SE PUEDEN REPETIR) --> SI JUSTO SE CONECTARON DOS A LAVEZ, ENCOLA PETICIONES...ENTONCES AL MOMENTO DE ESCRIBIR
        //NO SE VAN A COLISIONAR  .. UN OBJETO IPENDPOINT UNICO PARA CADA CLIENTE 

        //EL DICC CONCURRENTE TIENE LA LÓGICA DE ENCOLAR LAS PETICIONES PARA ARGEGAR DATOS AL DICC
        //LA KEY ES IPENDPOINT Y EL VALUE ES TIPO DE DATO INFODEUNCLIENTE




        public event EventHandler<ServidorEventArgs> NuevaConexion;
        public event EventHandler<ServidorEventArgs> ConexionTerminada;
        public event EventHandler<DatosRecibidosEventArgs> DatosRecibidos;

        public int PuertoDeEscucha { get; }  

        public Servidor(int puerto)
        {
            PuertoDeEscucha = puerto;   
        }

        public void Escuchar()
        {

            //instancio objeto tcplistener, le mando el puerto q se cargó en el constructor.
            listener = new TcpListener(IPAddress.Any, PuertoDeEscucha);

            // Inicio la escucha
            listener.Start();

            // Creo un thread para que se quede escuchando la llegada de los clientes sin interferir con la UI
            listenerThread = new Thread(EsperarCliente);
            //creo un nuevo thread, le pongo a ejectua el codigo de la funcion EsperarCliente, luego le digo que va a correr en background y 
            //una vez que tengo configurado el thread, q ya sabe lo que tiene que ejecutar en segundo plano, a ese thread lo pongo a funcionar con 
            // .start para que se ejecute dentro de un subproceso la funcion EsperarCliente




            // La siguiente línea hace que cuando se cierre la aplicación también se detenga el thread de escucha.
            // Ver: https://docs.microsoft.com/en-us/dotnet/api/system.threading.thread.isbackground
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        public void Cerrar()
        {
            // Recorro todos los clientes y voy cerrando las conexiones
            foreach (var cliente in clientes.Values)
            {
                // Cierro la conexión con el cliente
                cliente.Socket.Close();
            }
        }


        //este meotod esta diseñado para enviar un mismo mensaje a todos los clientes conectados al servidor
        public void EnviarDatos(string datos)

            //este metodo es el encargado de enviar el mensaje a todos los clientes conectados
            //recorre todos los clientes conectados almacenados en el diccionario concurrente llamado clientes
            //recordar que el dict mantiene la info de todos los clientes utilizando como key el IPEndPoint de cada cliente 
            //y como value una estructura InfoDeUnCliente que contiene el socket y thread asociado a cada cliente
            //Para cada cliente toma el socket de la struct y utiliza el metodo send para enviar los datos que recibe
            //como argumento en el metodo EnviarDatos,,y se codifican los datos en UTF-8 antes de envairlos



        {
            // Recorro todos los clientes conectados y les envío el mensaje en el parámetro datos

            //recorro el diccionario 
            foreach (var cliente in clientes.Values)
            {
                // Envío el mensaje codificado en UTF-8 (https://es.wikipedia.org/wiki/UTF-8)
                cliente.Socket.Send(Encoding.UTF8.GetBytes(datos));
            //se usa el encoding utf8 para codificar datos antes de enviarlos a través de un Socket
            }
        }

        

        private void EsperarCliente()
        {
            while (true)

                //defino la variable socket de tipo socket,  agarro el listener y le paso el método aceptSocket(), 
                //este método internamente queda haciendo un mirando en un loop infinito mirando al p8050, y leyendo ese puerto ,,
                //cuando un cliente llama, suena el método acept socket,,, y me devuelve un socket, con los datos del cliente que se almacena en la var socket
            {
                // Cuando se recibe la conexión, guardo la información del cliente
                // Guardo el socket que utilizó para mantener la conexión con el cliente
                var socket = listener.AcceptSocket(); // Se queda esperando la conexión de un cliente

                //** a esa variable socket a traves de una propiedad .RemoteEndPoint le pido el IpEndPoint q son los datos remotos del cliente...
                //esa propiedad me devuelve un objeto EndPoint,,,que es mas genérico (un IPendPoint es un EndPoint pero el EndPoint expone su interfaz algunas cosas)....
                //... si lo casteo a IPEndPoint, me va a mostrar mas datos entre ellos la ip y el puerto del cliente

                // Guardo el RemoteEndPoint, que utilizo para identificar al cliente
                // Casteo a IPEndPoint para poder obtener la IP y el puerto del cliente
                var endPoint = socket.RemoteEndPoint as IPEndPoint;
                //socket -->puntero del servidor al cliente
                //



                // Creo un thread para que se encargue de escuchar los mensajes del cliente.
                // Uso una función anónima para que el thread tenga acceso a la ip del cliente actual
                var thread = new Thread(() => LeerSocket(endPoint));

                //ejecuta leersocker, q el end point, q sabemosq es un ip endpoint.... 


                thread.IsBackground = true;  //confuguro el subproce ejecuto en background

                // Agrego la información del cliente al dictionary de clientes
                //el dicc concurrente llamado clientes le paso la key que es el IPendPoint  y le pego el elemento InfoDeUncliente
                clientes[endPoint] = new InfoDeUnCliente()
                {
                    //creo un elemento InfodeUnCliente
                    //le paso un vector con los dos elementos,,, en la variable socket d einfoDeUncliente quiero el socket q esta entrando acá y lo mismo con el thread.
                    Socket = socket,
                    Thread = thread,

                };

                // Disparo el evento NuevaConexion
                NuevaConexion?.Invoke(this, new ServidorEventArgs(endPoint));  

                // Inicio el thread encargado de escuchar los mensajes del cliente
                thread.Start();  //pone a ejecutar y leer socket en este subproce

                //luego volvemos a escuchar a la espera de un subproceso 
                //var thread = new Thread(() => LeerSocket(endPoint));
            }
        }

        private void LeerSocket(IPEndPoint endPoint)
        {
            //recibe el ip endpoint q tiene numero de ip y nuM de puerto  
            //genero un buffer de bytes ... un array de bytes de 100 elementos.. . .. . 
            //
            //

            var buffer = new byte[100]; // Array a utilizar para recibir los datos que llegan
            var cliente = clientes[endPoint]; // Información del cliente que se va a escuchar
            while (cliente.Socket.Connected)
            {

                //mientras connected me devuelve Verdadero .... 
                try
                {
                    // Me quedo esperando a que llegue un mensaje desde el cliente
                    int cantidadRecibida = cliente.Socket.Receive(buffer, buffer.Length, SocketFlags.None);    

                    // Me fijo cuántos bytes recibí. Si no recibí nada, entonces se desconectó el cliente
                    if (cantidadRecibida > 0)
                    {
                        //si es mayor a cero quiere decir que llegaron bytes, 

                        // Decodifico el mensaje recibido usando UTF-8 (https://es.wikipedia.org/wiki/UTF-8)
                        var datosRecibidos = Encoding.UTF8.GetString(buffer, 0, cantidadRecibida);
                        //encoding toma el vector ese y lo toma como un string 


                        // Disparo el evento de la recepción del mensaje
                        DatosRecibidos?.Invoke(this, new DatosRecibidosEventArgs(endPoint, datosRecibidos));
                    }
                    else
                    {
                        // Disparo el evento de la finalización de la conexión
                        ConexionTerminada?.Invoke(this, new ServidorEventArgs(endPoint));
                        break;
                    }
                }
                catch
                {
                    if (!cliente.Socket.Connected)
                    {
                        // Disparo el evento de la finalización de la conexión
                        ConexionTerminada?.Invoke(this, new ServidorEventArgs(endPoint));
                        break;
                    }
                }
            }
            // Elimino el cliente del dictionary que guarda la información de los clientes
            clientes.TryRemove(endPoint, out cliente);

            //al diccionario concurrente intenta remover ... si no funciona me da una excepcion 
            //
        }
    }
}

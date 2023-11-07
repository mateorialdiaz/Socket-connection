using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace EjemploCliente
{
    // Declaro una subclase de EventArgs para el evento DatosRecibidos
    public class DatosRecibidosEventArgs : EventArgs
    {  //esta clase se usa para pasar datos cuando se dispara el evento DatosRecibidos, contiene los datos recibidos del servidor 
       //


        public DatosRecibidosEventArgs(string datos)
        {
            DatosRecibidos = datos;
        }

        public string DatosRecibidos { get; set; }
    }

    public class Cliente
    {
        Socket socket; // Socket utilizado para enviar datos al Servidor y recibir datos del mismo
        Thread thread; // Thread utilizado para escuchar los mensajes enviados por el servidor

        // Flag utilizado para saber si el cliente está conectado al servidor o no.
        public bool Conectado { get; private set; }

        // Si estoy conectado esta propiedad nos permite obtener la IP y el puerto local
        public IPEndPoint LocalEndPoint
        {
            //Este es el punto de conexión local del socket.
            //En términos de red, un endpoint es una combinación de una dirección IP y un número de puerto.
            //En este caso, LocalEndPoint representaría la dirección IP y el puerto que tu cliente está usando en
            //la máquina local para establecer la conexión de socket.


            //as IPEndPoint: Intenta convertir el objeto devuelto por socket.LocalEndPoint a un tipo IPEndPoint.
            //Si la conversión es exitosa, devuelve el valor convertido; de lo contrario, devuelve null
            get { return socket?.LocalEndPoint as IPEndPoint; }
        }

        // Si estoy conectado esta propiedad nos permite obtener la IP y el puerto del servidor
        public IPEndPoint RemoteEndPoint
        {
            //Este es el punto de conexión remoto al que el socket está conectado. Esto significa la dirección IP
            //y el puerto del servidor al que tu cliente se ha conectado
            //Al igual que con LocalEndPoint, el operador ?. asegura que sólo se intente acceder a RemoteEndPoint
            //si socket no es null
            //



            get { return socket?.RemoteEndPoint as IPEndPoint; }
        }
        //arriba defino 2 propiedades de solo lectura diseñadas para exponer info. sobre los puntos de conexión de red (endpoints)
        //asociados con el socket que se utiliza para la comunicacion de red





        public event EventHandler ConexionTerminada;
        public event EventHandler<DatosRecibidosEventArgs> DatosRecibidos;



        //este metodo establece una conexión de red entre el cli y el serv:

        public void Conectar(string ip, int puerto)
        {

            //si el cliente ya esta conectado la funcion termina
            if (Conectado) return;

            // Creamos un socket con la configuración correcta para enviar y recibir datos sobre TCP
            // Ver: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.sockettype
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);


            //creo un objeto socket si no hay una conex existente:
            //*el socket es del tipo SocketType.Stream ("stream" se refiere a un tipo de socket que proporciona un flujo bidireccional y confiable de bytes)
            //y el otro parametro indica que es un socket TCP que 
            //proporciona una conexión orientada a la transmision de datos en forma de flujo de bytes



            //Intenta conectarse al servidor utilizando la dirección IP y el puerto proporcionados como parámetros (ip y puerto).
            //La función socket.Connect(ip, puerto) inicia una solicitud de conexión sincrónica al servidor especificado.
            //
            socket.Connect(ip, puerto);

            //ese socket q es remoto para el servidor, es local para el cliente


            //Establecimiento de la Bandera de Conexión
            Conectado = true;

            // Creo e inicio un thread para que escuche los mensajes enviados por el Servidor
            //la funcion de este hilo es escuchar continuamente los mensajes entrantes del servidor
            thread = new Thread(LeerSocket);
            thread.IsBackground = true; // se va a ejecutar en 2ndo plano...Background thread (https://docs.microsoft.com/en-us/dotnet/standard/threading/foreground-and-background-threads)
            thread.Start();


            //La función LeerSocket, que se inicia como parte de Conectar, es donde el cliente comienza a escuchar activamente los mensajes del servidor. Cualquier mensaje recibido se
            //procesa y se notifica a través del evento DatosRecibidos.


        }

        public void EnviarDatos(string datos)
        {
            //toma un string q se convierte a un formato q pueda ser transmitido a través de la red.
            //se usa UTF8 porque es una codificacion de caracteres universal q puede manejar cualquier caracter q este en el string

            //***Luego esos bytes son enviados a traves del socket que está conectado al servidor


            //*chequea para evitar que se intente enviar datos si el cliente no está conectado al servidor
            if (!Conectado) return;

            // Envío el mensaje codificado en UTF-8 (https://es.wikipedia.org/wiki/UTF-8)
            byte[] bytes = Encoding.UTF8.GetBytes(datos);
            socket?.Send(bytes);
        //el " ? " utiliza un operador de acceso adicional, conocido tambien como operador de propagacion nula
        //este operador se usa para llamar a metodo send sobre el objeto socket solo si socket no es null
        //o sea esta ultima linea verifica si socket existe, en ese caso llama a Send para enviar el array de bytes para parámetro
        
        }



        //"oreja" que se ejecuta en un hilo separado para no blpoquear el hijo principal de la aplicacion
        private void LeerSocket()
        {
            //es el responsable de la recepcion de los msj que vienen del servidor
            //se hace un bucle infinito q le permite revisar constantemente si hay nuevos mensajes que han llegado a través del socket
            //*


            // Declaro un array de bytes para contener los mensajes de entrada
            var buffer = new byte[100];
            while (true)
            {
                try
                {

                    //***en CantRecibida se almacena el numero de bytes que fueron recibidos del flujo de red y escritos en el bufer
                    //**socket es la instancia que se usa para la comunicacion de red, el socket debe estar conectado a un punto remoto (servidor) 
                    //para que pueda recibir datos de ese punto
                    //*.receive bloquea la ejecucion del hilo actual hasta que recibe datos del socket
                    //el array bufer actúa como almacenamiento temporal para los datos que se reciben a traves del socket
                    //buffer.Length: Indica la longitud del buffer, es decir, cuántos bytes pueden escribirse en el buffer. Se pasa este valor para
                    //decirle al método Receive el tamaño máximo de datos que espera recibir en esta operación.
                    //SocketFlags.None: Esta es una enumeración que proporciona un conjunto de modificadores que se pueden usar con llamadas a métodos de socket.
                    //SocketFlags.None indica que no se está utilizando ninguna bandera adicional para esta operación de recepción.




                    // Me quedo esperando a que llegue algún mensaje,,si no llega nada y la conexion esta intacta simplemente espera
                    //no consumiento cpu significativamente ya que le hilo está en estado de espera

                //en bufer se almacenan los datos recibidos , y con buffer.length indicamos el numero maximo de bytes a recibir
                //se intentará recibir tantos bytes como sea posible hasta llenar el buffer, en este caso de 100 
                    int cantidadRecibida = socket.Receive(buffer, buffer.Length, SocketFlags.None);
                    if (cantidadRecibida > 0)
                    {
                        // Decodifico el mensaje usando UTF-8 (https://es.wikipedia.org/wiki/UTF-8)
                        string mensaje = Encoding.UTF8.GetString(buffer, 0, cantidadRecibida);
                        // Disparo el evento DatosRecibidos, pasando como arg el mensaje que llegó desde el servidor
                        DatosRecibidos?.Invoke(this, new DatosRecibidosEventArgs(mensaje));
                    }
                    //Invoke se utiliza para ejecutar un delegado, que es un tipo que representa referencias a metodos
                    //los eventos en C# son manejados con delegados ,, permitiendo que los metodos sean adjuntados a eventos y llamados cuando estos suceden

                    //*Cuando Invoke se llama en un delegado de evento, como DatosRecibidos lo que hace es llamar a todos los 
                    //métodos que han sido adjuntados a ese evento. Si hay un metodo que maneja el evento DatosRecibidos, llamar a Invoke
                    //ejecutará ese método manejador
                    //


                    //en este caso los suscriptores serían métodos en otros componentes del programa que están interesados en saber 
                    //cuando el cliente recibe datos del servidor: en la carga del formulario se suscribe a los eventos de la clase cliente
                    // en "cliente.DatosRecibidos += Cliente_DatosRecibidos;" el formulario se suscribe al evento DatosRecibidos
                    //Cliente_datosRecibidos es otro método en ClienteForm que se llamará cuando el evento DatosRecibidos sea disparado




                    //llamada condicional a un evento, verifica si hay suscriptores al evento DatosRecibidos
                    //El operador de acceso condicional "?" solo invoca al metodo Invoke si DatosRecibidos no es null
                    //es decir si hay algun suscriptor ...si hay suscriptores se invoca al evento DatosRecibidos pasando la instancia actual
                    //(this o sea la instancia actual de Cliente) y un nuevo objeto DatosRecibidosEventArgs que contiene el mensaje decodificado



                }
                catch
                {
                    socket.Close();
                    break;
                }
            }

            Conectado = false;
            // Finalizó la conexión, por lo tanto genero el evento correspondiente
            ConexionTerminada?.Invoke(this, new EventArgs());
        }
    }
}




//***repaso eventos y suscripciones: 
//En C#, un evento es un mecanismo que permite a una clase notificar a otros componentes del
//programa cuando algo de interés ocurre. Los componentes interesados deben suscribirse al evento
//si quieren ser notificados cuando ocurre.
//
//Cuando se recibe un dato (DatosRecibidos), la clase Cliente "llama" a todos los métodos suscritos
//para informarles.
//

//Flujo de Ejecución con DatosRecibidos::

//1) En la clase Cliente, hay un evento definido llamado DatosRecibidos. También hay una clase especial
//DatosRecibidosEventArgs que hereda de EventArgs. Esta clase se utiliza para pasar información adicional
//cuando se dispara el evento. En este caso, lleva la cadena de los datos que se recibieron.
//
//2) cuando se carga el form, se instancia un cliente y el form se suscribe a sus eventos 
// el método Cliente_DatosRecibidos en ClienteForm será llamado cada vez que se dispare el evento DatosRecibidos
// en la instancia de Cliente.
//
//3)El metodo LeeSocket dispara el evento DatosRecibidos
//DatosRecibidos?. verifica si hay algún suscriptor al evento. Si no hay suscriptores, no hace nada.
//Invoke es el método que realmente dispara el evento. Llama a todos los métodos suscritos al evento (en este caso,
//Cliente_DatosRecibidos en ClienteForm). 
//
//this se refiere a la instancia actual de Cliente que está disparando el evento.
//new DatosRecibidosEventArgs(mensaje) crea una nueva instancia de DatosRecibidosEventArgs,
//pasando el mensaje recibido del servidor.
//
//4) Una vez que se dispara el evento, el método Cliente_DatosRecibidos en ClienteForm se ejecuta:
// Cliente_datosRecibidos en ClienteForm:
//
//    sender es el objeto que disparó el evento (en este caso, Cliente).
//e es el objeto DatosRecibidosEventArgs que contiene los datos que se recibieron del servidor.
//  Log es un método en ClienteForm que muestra el mensaje en el área de log del formulario.
//

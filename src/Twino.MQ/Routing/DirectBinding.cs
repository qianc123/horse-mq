using System;
using System.Threading.Tasks;
using Twino.MQ.Clients;
using Twino.Protocols.TMQ;

namespace Twino.MQ.Routing
{
    /// <summary>
    /// Direct message binding.
    /// Targets clients.
    /// Binding receivers are received messages as DirectMessage.
    /// </summary>
    public class DirectBinding : Binding
    {
        /// <summary>
        /// If direct binding is defined by client type or name, there might be multiple receivers.
        /// If True, only one receiver receives the message.
        /// If False, all receivers receive the message.
        /// </summary>
        public bool OnlyOneReceiver { get; set; }

        private DateTime _clientListUpdateTime;
        private MqClient[] _clients;

        /// <summary>
        /// Creates new direct binding.
        /// Name is the name of the binding.
        /// Target should be client id.
        /// If you want to target clients by name, target should be @name:client_name.
        /// Same usage available for client type @type:type_name.
        /// Content type the value how receiver will see the content type.
        /// Priority for router binding.
        /// </summary>
        public DirectBinding(string name, string target, ushort contentType, int priority, BindingInteraction interaction)
            : base(name, target, contentType, priority, interaction)
        {
        }

        /// <summary>
        /// Sends the message to binding receivers
        /// </summary>
        public override async Task<bool> Send(MqClient sender, TmqMessage message)
        {
            MqClient[] clients = GetClients();
            if (clients.Length == 0)
                return false;

            message.PendingAcknowledge = false;
            message.PendingResponse = false;
            message.ContentType = ContentType;

            if (Interaction == BindingInteraction.Acknowledge)
                message.PendingAcknowledge = true;
            else if (Interaction == BindingInteraction.Response)
                message.PendingResponse = true;

            bool atLeastOneSent = false;
            foreach (MqClient client in clients)
            {
                bool sent = await client.SendAsync(message);
                if (sent && !atLeastOneSent)
                    atLeastOneSent = true;
            }

            return atLeastOneSent;
        }

        private MqClient[] GetClients()
        {
            //todo: manage cache
            throw new NotImplementedException();
        }
    }
}
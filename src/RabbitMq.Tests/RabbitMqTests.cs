using System.Text;
using NUnit.Framework;
using RabbitMQ.Client;

namespace RabbitMq.Tests
{
    [TestFixture]
    public class RabbitMqTests
    {
        private readonly ConnectionFactory rabbitMqFactory = new ConnectionFactory { HostName = "localhost" };
        const string ExchangeName = "test.exchange";
        const string QueueName = "test.queue";

        [Test]
        public void Publish_persistent_message_to_test_exchange()
        {
            using (IConnection conn = rabbitMqFactory.CreateConnection())
            using (IModel channel = conn.CreateModel())
            {
                channel.ExchangeDeclare(ExchangeName, "direct", durable: true, autoDelete: false, arguments: null);
                
                channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(QueueName, ExchangeName, routingKey: QueueName);

                var props = channel.CreateBasicProperties();
                props.SetPersistent(true);

                var msgBody = Encoding.UTF8.GetBytes("Hello, World!");
                channel.BasicPublish(ExchangeName, routingKey: QueueName, basicProperties: props, body: msgBody);
            }
        }

        [Test]
        public void Get_message_from_queue()
        {
            using (IConnection conn = rabbitMqFactory.CreateConnection())
            using (IModel channel = conn.CreateModel())
            {
                BasicGetResult msgResponse = channel.BasicGet(QueueName, noAck: true);

                var msgBody = Encoding.UTF8.GetString(msgResponse.Body);
                Assert.That(msgBody, Is.EqualTo("Hello, World!"));
            }
        }

        [Test]
        public void Consume_message_from_Queue_Subscription()
        {
            using (IConnection conn = rabbitMqFactory.CreateConnection())
            using (IModel channel = conn.CreateModel())
            {
                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(QueueName, noAck: true, consumer: consumer);

                var msgResponse = consumer.Queue.Dequeue(); //blocking

                var msgBody = Encoding.UTF8.GetString(msgResponse.Body);
                Assert.That(msgBody, Is.EqualTo("Hello, World!"));
            }
        }

    }
}

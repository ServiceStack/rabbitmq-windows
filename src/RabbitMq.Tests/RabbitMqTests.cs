using System;
using System.IO;
using System.Text;
using System.Threading;
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
        public void Register_durable_Exchange_and_Queue()
        {            
            using (IConnection conn = rabbitMqFactory.CreateConnection())
            using (IModel channel = conn.CreateModel())
            {
                channel.ExchangeDeclare(ExchangeName, "direct", durable: true, autoDelete: false, arguments: null);

                channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(QueueName, ExchangeName, routingKey: QueueName);
            }
        }

        [Test]
        public void Publish_persistent_message_to_test_exchange()
        {
            using (IConnection conn = rabbitMqFactory.CreateConnection())
            using (IModel channel = conn.CreateModel())
            {
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
        public void Consume_one_message_from_Queue_Subscription()
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

        [Test]
        public void Publish_5_messages_to_test_exchange()
        {
            using (IConnection conn = rabbitMqFactory.CreateConnection())
            using (IModel channel = conn.CreateModel())
            {
                for (var i = 0; i < 5; i++)
                {
                    var props = channel.CreateBasicProperties();
                    props.SetPersistent(true);

                    var msgBody = Encoding.UTF8.GetBytes("Hello, World!");
                    channel.BasicPublish(ExchangeName, routingKey: QueueName, basicProperties: props, body: msgBody);
                }
            }
        }

        [Test]
        public void Consume_messages_from_Queue_Subscription()
        {
            using (IConnection conn = rabbitMqFactory.CreateConnection())
            using (IModel channel = conn.CreateModel())
            {
                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(QueueName, noAck: true, consumer: consumer);

                ThreadPool.QueueUserWorkItem(_ => {
                    var now = DateTime.UtcNow;
                    while (DateTime.UtcNow - now < TimeSpan.FromSeconds(5))
                    {
                        var props = channel.CreateBasicProperties();
                        props.SetPersistent(true);

                        var msgBody = Encoding.UTF8.GetBytes("Hello, World!");
                        channel.BasicPublish(ExchangeName, routingKey: QueueName, basicProperties: props, body: msgBody);

                        Thread.Sleep(1000);
                    }

                    channel.Close();
                });

                while (true)
                {
                    try
                    {
                        var msgResponse = consumer.Queue.Dequeue(); //blocking

                        var msgBody = Encoding.UTF8.GetString(msgResponse.Body);

                        Console.WriteLine("Received Message: " + msgBody);

                        Thread.Sleep(1000);
                    }
                    catch (EndOfStreamException ex)
                    {
                        Console.WriteLine("Channel was closed, Exiting...");
                        break;
                    }
                }
            }
        }

    }
}

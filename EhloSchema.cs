using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace GqlNet5Demo
{
    public sealed class EhloSchema : Schema
    {
        public EhloSchema(IServiceProvider provider) : base(provider)
        {
            Query = new EhloQuery();
            Mutation = new EhloMutation();
            Subscription = new EhloSubscription();
        }
    }

    public sealed class Message
    {
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class MessageType : ObjectGraphType<Message>
    {
        public MessageType()
        {
            Field(o => o.Content);
            Field(o => o.CreatedAt, type: typeof(DateTimeGraphType));
        }
    }

    public sealed class EhloQuery : ObjectGraphType
    {
        public EhloQuery()
        {
            Field<StringGraphType>("greet", description: "A type that returns a simple hello world string", resolve: context => "Hello, World");
            Field<MessageType>("greetComplex", description: "A type that returns a complex data", resolve: context =>
            {
                return new Message
                {
                    Content = "Hello, World",
                    CreatedAt = DateTime.UtcNow,
                };
            });
        }
    }

    public sealed class MessageInputType : InputObjectGraphType
    {
        public MessageInputType()
        {
            Field<StringGraphType>("content");
            Field<DateTimeGraphType>("createdAt");
        }
    }

    public sealed class EhloMutation : ObjectGraphType<object>
    {
        public EhloMutation()
        {
            Field<StringGraphType>("greetMe",
                    arguments: new QueryArguments(
                        new QueryArgument<StringGraphType>
                        {
                            Name = "name"
                        }),
                    resolve: context =>
                    {
                        string name = context.GetArgument<string>("name");
                        string message = $"Hello {name}!";
                        return message;
                    });

            Field<MessageType>("echoMessageComplex",
                    arguments: new QueryArguments(
                        new QueryArgument<MessageInputType>
                        {
                            Name = "message"
                        }),
                    resolve: context =>
                    {
                        Message message = context.GetArgument<Message>("message");
                        return message;
                    });
        }
    }

    public sealed class EhloSubscription : ObjectGraphType<object>
    {
        public ISubject<string> greetValues = new ReplaySubject<string>(1);

        public EhloSubscription()
        {
            AddField(new EventStreamFieldType
            {
                Name = "greetCalled",
                Type = typeof(StringGraphType),
                Resolver = new FuncFieldResolver<string>(context =>
                {
                    var message = context.Source as string;
                    return message;
                }),
                Subscriber = new EventStreamResolver<string>(context =>
                {
                    return greetValues.Select(message => message).AsObservable();
                }),
            });

            greetValues.OnNext("Hello, World");
        }
    }
}

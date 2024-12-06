using System;

namespace HTMLParserApp
{
    public class CustomQueue<T>
    {
        private class Node
        {
            public T Data { get; set; }
            public Node Next { get; set; }

            public Node(T data)
            {
                Data = data;
                Next = null;
            }
        }

        private Node front;
        private Node rear;
        public int Size { get; private set; }

        public CustomQueue()
        {
            front = null;
            rear = null;
            Size = 0;
        }

        public void Enqueue(T item)
        {
            Node newNode = new Node(item);

            if (rear == null)
            {
                front = rear = newNode;
            }
            else
            {
                rear.Next = newNode;
                rear = newNode;
            }
            Size++;
        }

        public T Dequeue()
        {
            if (IsEmpty())
            {
                throw new InvalidOperationException("Queue is empty");
            }

            T item = front.Data;
            front = front.Next;

            if (front == null)
            {
                rear = null;
            }
            Size--;

            return item;
        }

        public T Peek()
        {
            if (IsEmpty())
            {
                throw new InvalidOperationException("Queue is empty");
            }
            return front.Data;
        }

        public bool IsEmpty()
        {
            return Size == 0;
        }
    }
}
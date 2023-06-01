using System;
using System.Buffers;
using Orleans.Serialization.Buffers;
using Xunit;

namespace Orleans.Serialization.UnitTests
{
    [Trait("Category", "BVT")]
    public class PooledBufferTests
    {
        [Fact]
        public void LargeBufferRoundTrip()
        {
            var random = new Random();
            var buffer = new PooledBuffer();
            var randomData = new byte[1024 * 1024 * 10];
            random.NextBytes(randomData);
            buffer.Write(randomData);

            var slice4 = buffer.Slice(3000, 1500);
            var sliceArray4 = slice4.ToArray();
            Assert.Equal(randomData.AsSpan(3000, 1500).ToArray(), sliceArray4);

            var slice = buffer.Slice();
            var sliceArray = slice.ToArray();
            Assert.Equal(randomData, sliceArray);

            var slice3 = buffer.Slice(100, 1024);
            var sliceArray3 = slice3.ToArray();
            Assert.Equal(randomData.AsSpan(100, 1024).ToArray(), sliceArray3);

            var slice2 = buffer.Slice(100);
            var sliceArray2 = slice2.ToArray();
            var slicedRandomData = randomData.AsSpan(100).ToArray();
            Assert.Equal(slicedRandomData, sliceArray2);

            var rosArray = new byte[randomData.Length];
            buffer.AsReadOnlySequence().CopyTo(rosArray.AsSpan());
            Assert.Equal(randomData, rosArray);

            var spansArray = new byte[randomData.Length];
            var spansArraySpan = spansArray.AsSpan();
            foreach (var span in buffer.Slice())
            {
                span.CopyTo(spansArraySpan);
                spansArraySpan = spansArraySpan.Slice(span.Length);
            }
            Assert.Equal(randomData, spansArray);

            buffer.Dispose();
        }

        [Fact]
        public void LargeBufferRoundTrip_ReaderWriter()
        {
            var random = new Random();
            var randomData = new byte[1024 * 1024 * 10];
            random.NextBytes(randomData);
            var writer = Writer.Create(new PooledBuffer(), null);
            writer.Write(randomData);
            writer.Commit();
            var buffer = writer.Output;

            var slice = buffer.Slice();
            var sliceReader = Reader.Create(slice, null);
            var sliceArray = sliceReader.ReadBytes((uint)randomData.Length);
            Assert.Equal(randomData, sliceArray);

            var slice3 = buffer.Slice(100, 1024);
            var reader3 = Reader.Create(slice3, null);
            var result3 = reader3.ReadBytes((uint)slice3.Length);
            Assert.Equal(randomData.AsSpan(100, 1024).ToArray(), result3);

            var slice2 = buffer.Slice(100);
            var reader2 = Reader.Create(slice2, null);
            var result2 = reader2.ReadBytes((uint)slice2.Length);
            Assert.Equal(randomData.AsSpan(100).ToArray(), result2);

            var slice4 = buffer.Slice(3000, 1500);
            var reader4 = Reader.Create(slice4, null);
            var result4 = reader4.ReadBytes((uint)slice4.Length);
            Assert.Equal(randomData.AsSpan(3000, 1500).ToArray(), result4);

            var ros = buffer.AsReadOnlySequence();
            var rosReader = Reader.Create(ros, null);
            var rosArray = rosReader.ReadBytes((uint)randomData.Length);
            Assert.Equal(randomData, rosArray);
            
            buffer.Dispose();
        }
    }
}
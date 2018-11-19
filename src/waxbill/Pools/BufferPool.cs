using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Pools
{
    public abstract class BufferPool:PoolBase<ArraySegment<byte>>
    {

        private List<byte[]> global = new List<byte[]>();


        private readonly Int32 bufferSize;
        /// <summary>
        /// 每个Buffer的大小
        /// </summary>
        public Int32 BufferSize
        {
            get
            {
                return bufferSize;
            }
        }
        public BufferPool(Int32 bufferSize,Int32 increases,Int32 max):base(increases,max)
        {
            this.bufferSize = bufferSize;
        }

        protected override ArraySegment<byte>[] CreateItems(int suggestCount)
        {
            ArraySegment<byte>[] items = new ArraySegment<byte>[suggestCount];
            byte[] datas = new byte[suggestCount * this.bufferSize];
            global.Add(datas);
            for (int i = 0; i < suggestCount; i++)
            {
                items[i] = new ArraySegment<byte>(datas, i * suggestCount, bufferSize);
            }
            return items;
        }
    }
}

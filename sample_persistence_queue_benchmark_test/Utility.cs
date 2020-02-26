using System.IO;

namespace sample_persistence_queue_benchmark_test
{
    public class Utility
    {
        //時間節約のためWebからコピペしたコード
        public static long GetDirectorySize(DirectoryInfo dirInfo, bool isIncludeSubFolderTree=false)
        {
            long size = 0;

            //フォルダ内の全ファイルの合計サイズを計算する
            foreach (FileInfo fi in dirInfo.GetFiles())
                size += fi.Length;

            if (isIncludeSubFolderTree)
            {
                //サブフォルダのサイズを合計していく
                foreach (DirectoryInfo di in dirInfo.GetDirectories())
                    size += GetDirectorySize(di);
            }

            //結果を返す
            return size;
        }
    }
}
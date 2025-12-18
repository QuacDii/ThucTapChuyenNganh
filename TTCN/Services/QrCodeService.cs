using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TTCN.Services
{
    public static class QrCodeService
    {
        public static byte[] GenerateQrCode(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrData);
            using var bitmap = qrCode.GetGraphic(20);

            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }
}

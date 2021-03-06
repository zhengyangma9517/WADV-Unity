using System.Threading.Tasks;
using UnityEngine;
using WADV.MessageSystem;

namespace WADV.Plugins.Image {
    public class ImageCanvas : MonoMessengerBehaviour {
        private static readonly Rect FlipX = new Rect(1, 0, -1, 0);
        private static readonly Rect FlipY = new Rect(0, 1, 1, -1);
        private static readonly Rect FlipXy = new Rect(1, 1, -1, -1);
        
        public override int Mask { get; } = 1;
        public override bool IsStandaloneMessage { get; } = false;
        
        public override Task<Message> Receive(Message message) {
            var image = new GameObject();
            image.AddComponent<RectTransform>();
            image.transform.SetParent(GetComponent<RectTransform>());
            image.transform.SetSiblingIndex(400);
            return Task.FromResult(message);
        }
    }
}
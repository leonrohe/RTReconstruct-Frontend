import asyncio
from io import BytesIO
import websockets
import json
import uuid
from PIL import Image

# Store received fragments (for demo)
received_fragments = []

async def handle_connection(websocket):
    print("Client connected")

    try:
        async for message in websocket:
            # Decode the incoming message
            try:
                fragment = json.loads(message)

                # images = []
                # for CaptureDeviceFrame in fragment['Frames']:
                #     # Convert bytes to a BytesIO stream
                #     image_stream = BytesIO(bytes(CaptureDeviceFrame['Image']))
                    
                #     # Open the image
                #     img = Image.open(image_stream)
                    
                #     # Optional: load the image data to close the stream right after
                #     img.load()
                    
                #     # Append the image object to the list
                #     images.append(img)

                # for image in images:
                #     image.show()

                for extrinsic in fragment['Extrinsics']:
                    print(extrinsic) 

                received_fragments.append(fragment)

                # Simulate mesh reconstruction response
                mesh = {
                    "Status": "Success"
                }

                await websocket.send(json.dumps(mesh))
                print("Sent mesh back to client")

            except json.JSONDecodeError:
                print("Received non-JSON data. Ignoring.")

    except websockets.ConnectionClosed:
        print("Client disconnected")

async def main():
    server = await websockets.serve(handle_connection, "0.0.0.0", 8765)
    print("Server started on ws://localhost:8765")
    await server.wait_closed()

if __name__ == "__main__":
    asyncio.run(main())

import argparse
from PIL import Image, ImageOps
from PIL.Image import Resampling


def resize_and_pad(image_path, output_path, target_size=(256, 256), padding_color=(0, 0, 0)):
    # Open the original image
    with Image.open(image_path) as img:
        # Resize the image, maintaining aspect ratio
        img.thumbnail(target_size, Resampling.NEAREST)

        # Calculate padding to center the image
        delta_w = target_size[0] - img.width
        delta_h = target_size[1] - img.height
        padding = (delta_w // 2, delta_h // 2, delta_w - (delta_w // 2), delta_h - (delta_h // 2))

        # Add padding
        new_img = ImageOps.expand(img, padding, fill=padding_color)

        # Save the final image
        new_img.save(output_path)

def main():
    parser = argparse.ArgumentParser(description='Resize and pad images.')
    parser.add_argument('input_files', nargs='+', help='List of input image files')
    parser.add_argument('output_dir', help='Output directory for resized and padded images')
    parser.add_argument('--target_size', type=int, nargs=2, default=(256, 256), help='Target size for resizing (width height)')
    parser.add_argument('--padding_color', type=int, nargs=3, default=(0, 0, 0), help='Padding color (R G B)')

    args = parser.parse_args()

    for input_file in args.input_files:
        output_path = f"{args.output_dir}/{input_file.split('/')[-1]}"
        resize_and_pad(input_file, output_path, target_size=args.target_size, padding_color=tuple(args.padding_color))

if __name__ == '__main__':
    main()
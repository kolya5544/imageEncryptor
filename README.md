# ImageEncryptor

ImageEncryptor is a program allowing user to hide different types of data (just strings for now) inside pictures, encrypting them with a user-defined key.

## Installation

Use the most recent [release](https://github.com/kolya5544/imageEncryptor/releases) to install ImageEncryptor.

Make sure at least [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core) is installed
```bash
dotnet version
```

## Usage

### Encrypting
1. Find a picture, or create one. Depending on amounts of data you want to encrypt, you'll need different resolutions. Keep in mind, to have the best result, Width mod 3 should equal 0. For example, 99x99 picture can fit 200+ letters.
2. Open the program. Enter `1`. Enter the path to the picture.
3. Define a password or leave it empty.
4.1 Read warnings if there are any. Press ENTER to acknowledge every warning.
4.2 Define content to hide in a picture. It may be any text with any characters.
5. If there were no errors, output will be in `new.bmp`
### Decrypting
1. Open the program. Enter `2`. Enter the path to the picture or the file.
2. Enter password if there was any. Else, leave it empty.
3. Hidden contents of the file should appear.

## Contributing
Pull requests and code changes are welcome. For major changes, or new features, please open an issue first to discuss what you would like to change.

## Troubleshooting
### Most common errors
`System.Security.Cryptography.CryptographicException: 'Specified key is a known weak key for 'TripleDES' and cannot be used.'` - Please, use a password that is not a common password for hackers to prevent losing security factor of hiding data inside pictures.

`[!!!] Encrypted image content WILL differ from old image. Press ENTER to acknowledge.` - Not an error, but a warning. It means, that W % 4 == 0. Unfortunately, due to algorithm that we use to hide data inside pictures, we have to extend your picture 1 pixel to the right if W % 4 == 0.

`[!!!] Text to encode is too big to fit inside an image. Program will be stopped.` - Decrease text length and increase picture width. Make sure width % 3 equals 0, as it gets the best results.

`[---] Couldn't confirm the password.` - Wrong password. Try leaving it empty, or recheck if password is correct.

### F.A.Q


Q: `I've accidentally forgot the password for a picture! How do I retrieve my data?`

A: You can't. We use TripleDES to encrypt contents before hiding them


Q: `Does picture quality decrease after encrypting?`

A: No, the only quality loss is you can't have transparency in your pictures.


Q: `Program doesnt seem to run properly/doesn't start at all!`

A: Make sure that you have both [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core) and [.NET Framework]( https://dotnet.microsoft.com/download/dotnet-framework)


Q: `Do you support Windows XP/2000/98/95?`

A: No


## License
[MIT](https://choosealicense.com/licenses/mit/)

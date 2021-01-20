import {exec} from "child_process"
import aws from "aws-sdk"
import fs from "fs"
import path from "path"
import archiver from "archiver"
import {Connection} from "./model"


const buildPublish = (projectPath: string, outputPath: string, callback: (outPath: string, zipPath: string) => void ) => {
  exec(`dotnet publish "${projectPath}" -o "${outputPath}"`, (err, stdout, stderr) => {
    if(!err){
      callback(outputPath, path.join(__dirname, "../tmp/build.zip"));
    }
    else {
      console.log(err);
      console.log(stdout);
      console.log(stderr);
    }
  } )
}

const compress = (path: string, zipFileName: string, callback: (zipPath: string) => void) => {
  const output = fs.createWriteStream(zipFileName);
  const archive = archiver('zip', {
    gzip: true,
    zlib: {level: 9}
  });
  archive.pipe(output);
  const files = fs.readdirSync(path)
  files.forEach(f => {
    archive.file(f, {
      name: f
    });
    archive.finalize();
  })

  archive.on('end', () => {
    callback(zipFileName);
  } )
}

const pushToCloud = (zipPath: string, adapterId: string, entryAssembly: string, connection: Connection, callback: (err: string) => void ) => {
  const s3 = new aws.S3({
    accessKeyId: connection.accessKey,
    endpoint: connection.endpoint,
    secretAccessKey: connection.secretKey
  });

  s3.upload({
    Bucket: connection.bucket,
    ContentType: "application/zip",
    Key: `adapters/${adapterId.toLowerCase()}`,
    Body: fs.readFileSync(zipPath),
    Metadata: {
      "EntryAssembly": entryAssembly,
      "Lang": 'dotnet'
    }
  }, {}, (err, data) => {
    callback(err.message);
  })
}

const getEntryAssembly = (adapterPath: string) => {
  const fileName = path.parse(adapterPath).name;
  return `${fileName}.dll`
}

const cleanup = (cleanUpPath: string) => {
  fs.rmSync(cleanUpPath);
}

export default (adapterPath: string, adapterId: string, connection: Connection, callback: (result: string, isError: boolean) => void) => {
  const tmpPath = path.join(__dirname, "../tmp/build");
  buildPublish(adapterPath, tmpPath, (outPath, zipPath) => {
    compress(outPath, zipPath, (zipPath) => {
      pushToCloud(zipPath, adapterId, getEntryAssembly(adapterPath), connection, (err) => {
        cleanup(path.join(__dirname, "../tmp"));
        const result = err? err : `Successfully installer ${adapterId}`;
        callback(result, err? true : false);
      })
    })
  } )
}



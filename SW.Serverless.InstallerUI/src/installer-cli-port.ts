import {exec} from "child_process"
import aws from "aws-sdk"
import fs from "fs"
import path from "path"
import AdmZip from "adm-zip"
import {Connection} from "./model"


const buildPublish = (projectPath: string, outputPath: string, callback: (outPath: string) => void ) => {
  exec(`dotnet publish "${projectPath}" -o "${outputPath}"`, (err, stdout, stderr) => {
    if(!err){
      callback(outputPath);
    }
    else {
      console.log(err);
      console.log(stdout);
      console.log(stderr);
    }
  } )
}

const compress = (path: string, zipFileName: string, callback: (err: string) => void) => {
  const zip = new AdmZip();

  const files = fs.readdirSync(path)
  files.forEach((f: any) => {
    zip.addLocalFile(`${path}/${f}`);
  })

  zip.writeZip(zipFileName, (err) => callback(err?.message) )

  console.log("Finalizing..");

}

const pushToCloud = (zipPath: string, adapterId: string, entryAssembly: string, connection: Connection, callback: (err: string) => void ) => {
  const s3 = new aws.S3({
    accessKeyId: connection.accessKey,
    endpoint: connection.endpoint,
    secretAccessKey: connection.secretKey
  });

  const blob = fs.readFileSync(zipPath);

  s3.upload({
    Bucket: connection.bucket,
    ContentType: "application/zip",
    Key: `adapters/${adapterId.toLowerCase()}`,
    Body: blob,
    Metadata: {
      "EntryAssembly": entryAssembly,
      "Lang": 'dotnet'
    }
  }, {}, (err, data) => {
    callback(err?.message);
  })
}

const getEntryAssembly = (adapterPath: string) => {
  const fileName = path.parse(adapterPath).name;
  return `${fileName}.dll`
}

const cleanup = (cleanUpPath: string) => {
  //fs.rmdirSync(cleanUpPath, {recursive: true} );
}

export default (adapterPath: string, adapterId: string, connection: Connection, callback: (result: string, isError: boolean) => void) => {
  const tmpPath = path.join(__dirname, "../tmp/build");
  const zipPath = path.join(__dirname, "../tmp/build.zip");

  buildPublish(adapterPath, tmpPath, (outPath) => {
    compress(outPath, zipPath, () => {

      pushToCloud(zipPath, adapterId, getEntryAssembly(adapterPath), connection, (err) => {
        cleanup(path.join(__dirname, "../tmp"));
        const result = err? err : `Successfully installed ${adapterId}`;
        callback(result, err? true : false);
      })
    })
  })
}



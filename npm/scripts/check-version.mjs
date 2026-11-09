// The package major tracks the Angular major so consumers can match versions at a glance.
import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const versionFile = resolve(root, 'version.json');
const libPackageFile = resolve(root, 'projects/bpits/results-ngx/package.json');

const fail = (message) => {
  console.error(`check-version: ${message}`);
  process.exit(1);
};

const readJson = (file) => {
  try {
    return JSON.parse(readFileSync(file, 'utf8'));
  } catch (e) {
    return fail(`could not read ${file}: ${e.message}`);
  }
};

const version = readJson(versionFile).version;
const range = readJson(libPackageFile).peerDependencies?.['@angular/core'];
if (typeof version !== 'string' || !/^\d+/.test(version)) fail(`no valid "version" in ${versionFile}`);
if (typeof range !== 'string') fail(`no peerDependencies["@angular/core"] in ${libPackageFile}`);

// Use the lowest major: it is the oldest Angular the package promises to support.
const majors = range
  .split('||')
  .map((part) => /(\d+)/.exec(part)?.[1])
  .filter((m) => m !== undefined)
  .map(Number);
if (majors.length === 0) fail(`cannot parse an Angular major from "${range}"`);
const angularMajor = Math.min(...majors);
const packageMajor = Number(/^\d+/.exec(version)[0]);

if (packageMajor !== angularMajor) {
  fail(
    `npm/version.json is "${version}" (major ${packageMajor}) but @angular/core peer range "${range}" ` +
      `means Angular ${angularMajor}. Bump npm/version.json "version" to "${angularMajor}.0".`
  );
}
console.log(`check-version: OK (package major ${packageMajor} = Angular major ${angularMajor})`);

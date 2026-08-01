const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const Ajv2020 = require('ajv/dist/2020').default;
const addFormats = require('ajv-formats');
const YAML = require('yaml');

const root = path.resolve(__dirname, '..');
const schemaDir = path.join(root, 'schemas');
const schemas = fs.readdirSync(schemaDir)
  .filter((name) => name.endsWith('.schema.json'))
  .map((name) => ({ name, value: JSON.parse(fs.readFileSync(path.join(schemaDir, name), 'utf8')) }));

function validator() {
  const ajv = new Ajv2020({ allErrors: true, strict: true });
  addFormats(ajv);
  for (const schema of schemas) ajv.addSchema(schema.value);
  return ajv;
}

test('all JSON schemas compile in strict draft-2020-12 mode', () => {
  const ajv = validator();
  for (const schema of schemas) assert.doesNotThrow(() => ajv.getSchema(schema.value.$id));
});

test('signed match-result example conforms to the public contract', () => {
  const ajv = validator();
  const schema = schemas.find((entry) => entry.name === 'match-result.schema.json').value;
  const example = JSON.parse(fs.readFileSync(path.join(root, 'examples', 'match-result.json'), 'utf8'));
  const validate = ajv.getSchema(schema.$id);
  assert.equal(validate(example), true, JSON.stringify(validate.errors));
});

test('client match ticket is opaque and rejects a leaked map seed', () => {
  const ajv = validator();
  const schema = schemas.find((entry) => entry.name === 'match-ticket.schema.json').value;
  const example = JSON.parse(fs.readFileSync(path.join(root, 'examples', 'match-ticket.json'), 'utf8'));
  const validate = ajv.getSchema(schema.$id);
  assert.equal(validate(example), true, JSON.stringify(validate.errors));
  assert.equal('map_seed' in example.payload, false);
  const leaked = structuredClone(example);
  leaked.payload.map_seed = '42';
  assert.equal(validate(leaked), false);
  assert.ok(validate.errors.some((error) => error.keyword === 'additionalProperties'));
});

test('OpenAPI document is parseable and each operation has an id', () => {
  const spec = YAML.parse(fs.readFileSync(path.join(root, 'openapi', 'sandsunder-backend.v1.yaml'), 'utf8'));
  assert.equal(spec.openapi, '3.1.0');
  const ids = [];
  for (const pathItem of Object.values(spec.paths)) {
    for (const method of ['get', 'post', 'put', 'patch', 'delete']) {
      if (pathItem[method]) ids.push(pathItem[method].operationId);
    }
  }
  assert.equal(ids.length, 4);
  assert.equal(new Set(ids).size, ids.length);
  assert.ok(ids.every(Boolean));
});

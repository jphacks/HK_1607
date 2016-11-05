class CreateUsers < ActiveRecord::Migration[5.0]
  def change
    create_table :users do |t|
      t.string :login_id
      t.string :student_name
      t.string :password
      t.boolean :connected_flag
      t.boolean :teacher_flag
      t.boolean :admin_flag

      t.timestamps
    end
  end
end
